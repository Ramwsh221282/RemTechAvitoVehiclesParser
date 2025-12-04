using PuppeteerSharp;
using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class CataloguePagesProcessingBackgroundTask(
    NpgSqlDataSourceFactory dataSourceFactory,
    Serilog.ILogger logger,
    BrowserFactory browserFactory,
    AvitoBypassFactory bypassFactory
    ) : 
    ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<CataloguePagesProcessingBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using IPostgreSqlAdapter adapter = await dataSourceFactory.CreateAdapter(ct);
        NpgSqlParserWorkStagesStorage workStages = new(adapter);
        NpgSqlPaginationEvaluationParsersStorage parsers = new(adapter);
        NpgSqlCataloguePageUrlsStorage urls = new(adapter);
        NpgSqlCataloguePageItemsStorage itemsStorage = new(adapter);
        await adapter.UseTransaction(ct);
        
        ParserWorkStageQuery workStageQuery = new(Name: WorkStageConstants.CatalogueStageName, WithLock: true);
        Maybe<ParserWorkStage> stage = await workStages.GetWorkStage(workStageQuery, ct);
        if (!stage.HasValue) return;
            
        PaginationEvaluationParsersQuery parsersQuery = new(
            ParserId: stage.Value.GetSnapshot().Id,
            LinksWithCurrentPage: true,
            LinksWithMaxPage: true,
            OnlyNotProcessedLinks: true,
            LinksLimit: 1,
            WithLock: true);
        
        Maybe<PaginationEvaluationParser> parser = await parsers.GetParser(parsersQuery, ct);
        if (!parser.HasValue) return;
        
        PaginationEvaluationParserSnapshot parserSnapshot = parser.Value.GetSnapshot();
        PaginationEvaluationParserLinkSnapshot link = parserSnapshot.Links.First();
        CataloguePageUrlQuery pageUrlQuery = new(
            LinkId: link.Id, 
            UnprocessedOnly: true, 
            WithLock: true,
            Limit: 1);
            
        Maybe<CataloguePageUrl> pageUrl = await urls.GetSingle(pageUrlQuery, ct);
        if (!pageUrl.HasValue)
        {
            ParserWorkStage concreteItemStage = stage.Value.ChangeStage(new ParserWorkStage.ConcreteItemWorkStage(stage.Value));
            await workStages.Update(concreteItemStage, ct);
            ParserWorkStageSnapshot concreteItemStageSnapshot = concreteItemStage.GetSnapshot();
            _logger.Information(
                """
                No unprocessed catalogue page urls left.
                Switched to concrete items processing stage:
                Id: {Id}
                Name: {Name}
                """,
                concreteItemStageSnapshot.Id, concreteItemStageSnapshot.Name);
            await CommitTransaction(adapter, ct);
            return;   
        }
        
        try
        {
            CataloguePageUrl processed = await ProcessUrl(pageUrl.Value);
            CataloguePageItem[] items = processed.Items().ToArray();
                    
            int inserted = await itemsStorage.InsertMany(items);
            await urls.Update(processed, ct);
                    
            CataloguePageUrlSnapshot processedSnapshot = processed.GetSnapshot();
            object[] logProperties = 
            [
                processedSnapshot.Id, 
                processedSnapshot.Url, 
                processedSnapshot.LinkId, 
                processedSnapshot.Processed,
                inserted
            ];
        
            _logger.Information(
                """
                Processed catalogue url info:
                ID: {Id}
                Url: {Url}
                Link id: {LinkId}
                Processed: {Processed}
                Items amount: {Items} 
                """,
                logProperties);
        }
        catch (Exception ex)
        {
            CataloguePageUrl retryCountIncreased = pageUrl.Value.IncremenetRetryCount();
            CataloguePageUrlSnapshot retrySnap = retryCountIncreased.GetSnapshot();
            _logger.Error(
                ex,
                "Error at processing catalogue page url {Url}. Retries: {Count}",
                retrySnap.Url,
                retrySnap.RetryCount);
            await urls.Update(retryCountIncreased, ct);
        }

        await CommitTransaction(adapter, ct);
    }

    private async Task CommitTransaction(IPostgreSqlAdapter adapter, CancellationToken ct)
    {
        try
        {
            await adapter.CommitTransaction(ct);
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
        CataloguePageUrlSnapshot snapshot = url.GetSnapshot();
        IBrowser browser = await browserFactory.ProvideBrowser(headless: false);
        
        try
        {
            await (await browser.GetPage()).NavigatePage(snapshot.Url);
            if (!await bypassFactory.Create(await browser.GetPage()).Bypass())
            {
                _logger.Warning("Failed to process parser link: {Url}. Unable to resolve captcha.", snapshot.Url);
                return url;
            }
            
            await (await browser.GetPage()).ScrollBottom();
            await (await browser.GetPage()).ScrollTop();

            IElementHandle[] catalogueElements = await GetCatalogueElements(await browser.GetPage());
            List<CataloguePageItem> items = [];
            await foreach ((string Id, string Url, IReadOnlyList<string> Photos) data in GetCatalogueItemsMetadata(catalogueElements, await browser.GetPage()))
                items.Add(CataloguePageItem.New(data.Id, snapshot.Id, data.Url, data.Photos));

            CataloguePageUrl processed = CataloguePageUrl.FromSnapshot(snapshot).MarkProcessed();
            processed.AddItems(items);
            return processed;
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