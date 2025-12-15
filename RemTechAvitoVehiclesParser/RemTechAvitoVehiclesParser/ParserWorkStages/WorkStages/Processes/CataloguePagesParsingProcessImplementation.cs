using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class CataloguePagesParsingProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess CatalogueProcess =>
            async (deps, ct) =>
            {
                Serilog.ILogger logger = deps.Logger.ForContext<WorkStageProcess>();
                await using NpgSqlSession session = new(deps.NpgSql);
                NpgSqlPaginationParsingParsersStorage parsersStorage = new(session);
                NpgSqlCataloguePageUrlsStorage urls = new(session);
                NpgSqlCataloguePageItemsStorage itemsStorage = new(session);
                await session.UseTransaction(ct);

                WorkStageQuery stageQuery = new(
                    Name: WorkStageConstants.CatalogueStageName,
                    WithLock: true
                );

                Maybe<ParserWorkStage> stage = await ParserWorkStage.GetSingle(
                    session,
                    stageQuery,
                    ct
                );
                if (!stage.HasValue)
                    return;

                PaginationEvaluationParsersQuery parserQuery = new(
                    ParserId: stage.Value.Id,
                    LinksWithCurrentPage: true,
                    LinksWithMaxPage: true,
                    OnlyNotProcessedLinks: true,
                    LinksLimit: 1,
                    WithLock: true
                );

                Maybe<ProcessingParser> parser = await parsersStorage.GetParser(parserQuery, ct);
                if (!parser.HasValue)
                    return;

                ProcessingParserLink link = parser.Value.Links[0];
                CataloguePageUrlQuery pageUrlQuery = new(
                    LinkId: link.Id,
                    UnprocessedOnly: true,
                    WithLock: true,
                    Limit: 1
                );

                Maybe<CataloguePageUrl> pageUrl = await urls.GetSingle(pageUrlQuery, ct);
                if (!pageUrl.HasValue)
                {
                    ParserWorkStage concreteItemStage = stage.Value.ChangeStage(
                        new ConcreteItemWorkStage(stage.Value)
                    );
                    await concreteItemStage.Update(session, ct);
                    logger.Information(
                        """
                        No unprocessed catalogue page urls left.
                        Switched to concrete items processing stage:
                        Id: {Id}
                        Name: {Name}
                        """,
                        concreteItemStage.Id,
                        concreteItemStage.Name
                    );
                    await session.UnsafeCommit(ct);
                    return;
                }

                CataloguePageUrl target = pageUrl.Value;

                try
                {
                    target = await ProcessUrl(target, deps);
                    await itemsStorage.InsertMany(target.Items);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error at processing page. {Url}", target.Url);
                    target = target.IncrementRetryCount();
                }
                finally
                {
                    await urls.Update(target, ct);
                }

                try
                {
                    await session.UnsafeCommit(ct);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error at committing transaction");
                }
            };
    }

    private static async Task<CataloguePageUrl> ProcessUrl(
        CataloguePageUrl url,
        WorkStageProcessDependencies deps
    )
    {
        IBrowser browser = await deps.Browsers.ProvideBrowser(headless: false);

        try
        {
            await (await browser.GetPage()).NavigatePage(url.Url);
            if (!await deps.Bypasses.Create(await browser.GetPage()).Bypass())
                return url;

            await (await browser.GetPage()).ScrollBottom();
            await (await browser.GetPage()).ScrollTop();

            IElementHandle[] catalogueElements = await GetCatalogueElements(
                await browser.GetPage()
            );
            List<CataloguePageItem> items = [];
            await foreach (
                (string Id, string Url, IReadOnlyList<string> Photos) in GetCatalogueItemsMetadata(
                    catalogueElements,
                    await browser.GetPage()
                )
            )
                items.Add(CataloguePageItem.New(Id, url.Id, Url, Photos));

            return url.MarkProcessed().AddItems(items);
        }
        catch
        {
            throw;
        }
        finally
        {
            await browser.DestroyAsync();
        }
    }

    private static async Task<IElementHandle[]> GetCatalogueElements(IPage page)
    {
        Maybe<IElementHandle> rootItemsContainer = await page.GetElementRetriable(
            "div.index-root-H81wX"
        );
        if (!rootItemsContainer.HasValue)
            return [];
        IElementHandle[] catalogueItems = await rootItemsContainer.Value.GetElements(
            "div[data-marker='item']"
        );
        return catalogueItems;
    }

    private static async IAsyncEnumerable<(
        string Id,
        string Url,
        IReadOnlyList<string> Photos
    )> GetCatalogueItemsMetadata(IElementHandle[] catalogueItems, IPage page)
    {
        foreach (IElementHandle catalogueItem in catalogueItems)
        {
            Maybe<string> itemId = await catalogueItem.GetAttribute("data-item-id");
            if (!itemId.HasValue)
                continue;

            Maybe<IElementHandle> titleContainer = await catalogueItem.GetElementRetriable(
                "div.iva-item-listTopBlock-n6Rva"
            );
            if (!titleContainer.HasValue)
                continue;

            Maybe<IElementHandle> itemUrlContainer = await titleContainer.Value.GetElementRetriable(
                "a[itemprop='url']"
            );
            if (!itemUrlContainer.HasValue)
                continue;

            Maybe<string> itemUrlAttribueValue = await itemUrlContainer.Value.GetAttribute("href");
            if (!itemUrlAttribueValue.HasValue)
                continue;

            Maybe<IElementHandle> itemImage = await catalogueItem.GetElementRetriable(
                "div[data-marker='item-image']"
            );
            if (!itemImage.HasValue)
                continue;
            await itemImage.Value.HoverAsync();

            Maybe<IElementHandle> updatedItemImage = await page.GetElementRetriable(
                $"div[data-marker='item'][data-item-id='{itemId.Value}']"
            );
            if (!updatedItemImage.HasValue)
                continue;

            Maybe<IElementHandle> photoSliderList =
                await updatedItemImage.Value.GetElementRetriable("ul.photo-slider-list-R0jle");
            if (!photoSliderList.HasValue)
                continue;

            IElementHandle[] photoElements = await photoSliderList.Value.GetElements("li");
            IReadOnlyList<string> photos = await GetItemPhotos(photoElements);

            string itemIdValue = itemId.Value;
            string itemUrlValue = $"https://avito.ru{itemUrlAttribueValue.Value}";
            yield return (itemIdValue, itemUrlValue, photos);
        }
    }

    private static async Task<IReadOnlyList<string>> GetItemPhotos(IElementHandle[] photoElements)
    {
        List<string> photos = [];
        foreach (IElementHandle photo in photoElements)
        {
            Maybe<IElementHandle> imageElement = await photo.GetElementRetriable("img");
            if (!imageElement.HasValue)
                continue;
            Maybe<string> srcSetAttribute = await imageElement.Value.GetAttribute("srcset");
            if (!srcSetAttribute.HasValue)
                continue;
            string[] sets = srcSetAttribute.Value.Split(',');
            string highQualityImageUrl = sets[^1].Split(' ')[0];
            photos.Add(highQualityImageUrl);
        }

        return photos;
    }
}
