using ParsingSDK.Parsing;
using PuppeteerSharp;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class CataloguePagesPagesParsingBackgroundTask(
    NpgSqlConnectionFactory connectionFactory,
    Serilog.ILogger logger,
    BrowserFactory browserFactory,
    AvitoBypassFactory bypassFactory
    ) : 
    ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<CataloguePagesPagesParsingBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using NpgSqlSession session = new NpgSqlSession(connectionFactory);
        NpgSqlParserWorkStagesStorage workStages = new(session);
        NpgSqlPaginationParsingParsersStorage parsersStorage = new(session);
        NpgSqlCataloguePageUrlsStorage urls = new(session);
        NpgSqlCataloguePageItemsStorage itemsStorage = new(session);
        await session.UseTransaction(ct);
        
        WorkStageQuery workStageQuery = new(Name: WorkStageConstants.CatalogueStageName, WithLock: true);
        Maybe<ParserWorkStage> stage = await workStages.GetWorkStage(workStageQuery, ct);
        if (!stage.HasValue) return;
            
        PaginationEvaluationParsersQuery parsersQuery = new(
            ParserId: stage.Value.Id,
            LinksWithCurrentPage: true,
            LinksWithMaxPage: true,
            OnlyNotProcessedLinks: true,
            LinksLimit: 1,
            WithLock: true);
        
        Maybe<PaginationParsingParser> parser = await parsersStorage.GetParser(parsersQuery, ct);
        if (!parser.HasValue) return;
        
        PaginationParsingParserLink link = parser.Value.Links.First();
        CataloguePageUrlQuery pageUrlQuery = new(
            LinkId: link.Id, 
            UnprocessedOnly: true, 
            WithLock: true,
            Limit: 1);
            
        Maybe<CataloguePageUrl> pageUrl = await urls.GetSingle(pageUrlQuery, ct);
        if (!pageUrl.HasValue)
        {
            ParserWorkStage concreteItemStage = stage.Value.ChangeStage(new ConcreteItemWorkStage(stage.Value));
            await workStages.Update(concreteItemStage, ct);
            _logger.Information(
                """
                No unprocessed catalogue page urls left.
                Switched to concrete items processing stage:
                Id: {Id}
                Name: {Name}
                """,
                concreteItemStage.Id, concreteItemStage.Name);
            await CommitTransaction(session, ct);
            return;   
        }
        
        try
        {
            CataloguePageUrl processed = await ProcessUrl(pageUrl.Value);
            await itemsStorage.InsertMany(processed.Items);
            await urls.Update(processed, ct);
        
            _logger.Information(
                """
                Processed catalogue url info:
                ID: {Id}
                Url: {Url}
                Link id: {LinkId}
                Processed: {Processed}
                Items amount: {Items} 
                """,
                processed.Id, processed.Url, processed.LinkId, processed.Processed, processed.Items.Count);
        }
        catch (Exception ex)
        {
            CataloguePageUrl retryCountIncreased = pageUrl.Value.IncrementRetryCount();
            _logger.Error(
                ex,
                "Error at processing catalogue page url {Url}. Retries: {Count}",
                retryCountIncreased.Url,
                retryCountIncreased.RetryCount);
            await urls.Update(retryCountIncreased, ct);
        }

        await CommitTransaction(session, ct);
    }

    private async Task CommitTransaction(NpgSqlSession adapter, CancellationToken ct)
    {
        try
        {
            await adapter.UnsafeCommit(ct);
        }
        catch(Exception ex)
        {
            _logger.Error(ex, "Error at commiting transaction.");
        }
    }
    
    private async Task<CataloguePageUrl> ProcessUrl(
        CataloguePageUrl url
        )
    {
        IBrowser browser = await browserFactory.ProvideBrowser(headless: false);
        
        try
        {
            await (await browser.GetPage()).NavigatePage(url.Url);
            if (!await bypassFactory.Create(await browser.GetPage()).Bypass())
            {
                _logger.Warning("Failed to process parser link: {Url}. Unable to resolve captcha.", url.Url);
                return url;
            }
            
            await (await browser.GetPage()).ScrollBottom();
            await (await browser.GetPage()).ScrollTop();

            IElementHandle[] catalogueElements = await GetCatalogueElements(await browser.GetPage());
            List<CataloguePageItem> items = [];
            await foreach ((string Id, string Url, IReadOnlyList<string> Photos) data in GetCatalogueItemsMetadata(catalogueElements, await browser.GetPage()))
                items.Add(CataloguePageItem.New(data.Id, url.Id, data.Url, data.Photos));

            CataloguePageUrl processed = url.MarkProcessed();
            return processed.AddItems(items);
        }
        finally
        {
            await browser.DestroyAsync();
        }
    }

    private static async Task<IElementHandle[]> GetCatalogueElements(IPage page)
    {
        Maybe<IElementHandle> rootItemsContainer = await page.GetElementRetriable("div.index-root-H81wX");
        if (!rootItemsContainer.HasValue) return [];
        IElementHandle[] catalogueItems = await rootItemsContainer.Value.GetElements("div[data-marker='item']");
        return catalogueItems;
    }
    
    private static async IAsyncEnumerable<(string Id, string Url, IReadOnlyList<string> Photos)> GetCatalogueItemsMetadata(
        IElementHandle[] catalogueItems, 
        IPage page)
    {
        foreach (IElementHandle catalogueItem in catalogueItems)
        {
            Maybe<string> itemId = await catalogueItem.GetAttribute("data-item-id");
            if (!itemId.HasValue) continue;

            Maybe<IElementHandle> titleContainer = await catalogueItem.GetElementRetriable("div.iva-item-listTopBlock-n6Rva");
            if (!titleContainer.HasValue) continue;

            Maybe<IElementHandle> itemUrlContainer = await titleContainer.Value.GetElementRetriable("a[itemprop='url']");
            if (!itemUrlContainer.HasValue) continue;

            Maybe<string> itemUrlAttribueValue = await itemUrlContainer.Value.GetAttribute("href");
            if (!itemUrlAttribueValue.HasValue) continue;
        
            Maybe<IElementHandle> itemImage = await catalogueItem.GetElementRetriable("div[data-marker='item-image']");
            if (!itemImage.HasValue) continue;
            await itemImage.Value.HoverAsync();

            Maybe<IElementHandle> updatedItemImage = await page.GetElementRetriable($"div[data-marker='item'][data-item-id='{itemId.Value}']");
            if (!updatedItemImage.HasValue) continue;
            
            Maybe<IElementHandle> photoSliderList = await updatedItemImage.Value.GetElementRetriable("ul.photo-slider-list-R0jle");
            if (!photoSliderList.HasValue) continue;

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
            if (!imageElement.HasValue) continue;
            Maybe<string> srcSetAttribute = await imageElement.Value.GetAttribute("srcset");
            if (!srcSetAttribute.HasValue) continue;
            string[] sets = srcSetAttribute.Value.Split(',');
            string highQualityImageUrl = sets[^1].Split(' ')[0];
            photos.Add(highQualityImageUrl);
        }

        return photos;
    }
}