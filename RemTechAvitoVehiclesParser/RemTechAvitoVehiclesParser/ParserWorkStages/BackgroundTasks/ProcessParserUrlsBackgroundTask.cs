using PuppeteerSharp;
using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.Parsing.FirewallBypass;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class ProcessParserUrlsBackgroundTask(
    NpgSqlDataSourceFactory dataSourceFactory,
    Serilog.ILogger logger,
    BrowserFactory browserFactory
    ) : 
    ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ProcessParserUrlsBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        // _logger.Information("Starting processing parser url background task.");
        // CancellationToken ct = context.CancellationToken;
        // await using (IPostgreSqlAdapter adapter = await dataSourceFactory.CreateAdapter(ct))
        // {
        //     await adapter.UseTransaction(ct);
        //     NpgSqlParserWorkStagesStorage workStages = new(adapter);
        //     NpgSqlPaginationEvaluationParsersStorage parsers = new(adapter);
        //     ParserWorkStageQuery workStageQuery = new(Name: WorkStageConstants.CatalogueStageName);
        //     Maybe<ParserWorkStage> stage = await workStages.GetWorkStage(workStageQuery, ct);
        //     if (!stage.HasValue)
        //     {
        //         _logger.Information("Stopping processing parser url background task. No catalogue stage exists.");
        //         return;
        //     }
        //
        //     PaginationEvaluationParsersQuery parsersQuery = new(
        //         ParserId: stage.Value.GetSnapshot().Id, 
        //         LinksWithCurrentPage: true, 
        //         LinksWithMaxPage: true,
        //         OnlyNotProcessedLinks: true,
        //         LinksLimit: 1);
        //     
        //     Maybe<PaginationEvaluationParser> parser = await parsers.GetParser(parsersQuery, ct);
        //     if (!parser.HasValue)
        //     {
        //         _logger.Information("Stopping processing parser url background task. No unprocessed links with pagination initialized.");
        //         return;
        //     }
        //
        //     PaginationEvaluationParserSnapshot parserSnapshot = parser.Value.GetSnapshot();
        //     try
        //     {
        //         foreach (var link in parser.Value.Links())
        //         {
        //             
        //             
        //             link.MarkProcessed();
        //         }
        //     }
        //     catch(Exception ex)
        //     {
        //         _logger.Error(ex, "Error processing parser url link in background task.");
        //         
        //     }
        //     
        //     await ProcessLink(parser.Value, parsers, ct);
        // }
    }

    private async Task ProcessLink(
        PaginationEvaluationParser parser,
        NpgSqlPaginationEvaluationParsersStorage parsers,
        CancellationToken ct
        )
    {
        PaginationEvaluationParserSnapshot parserSnapshot = parser.GetSnapshot();
        foreach (PaginationEvaluationParserLinkSnapshot link in parserSnapshot.Links)
        {
            await using IBrowser browser = await browserFactory.ProvideBrowser(headless: false);
            await using IPage page = await browser.GetPage();
            await page.NavigatePage(link.Url);
            bool solved = await new AvitoByPassFirewallWithRetry(new AvitoBypassFirewallLazy(page, new AvitoBypassFirewall(page))).Bypass();
            if (!solved)
            {
                _logger.Warning("Failed to process parser link: {Url}. Unable to resolve captcha.", link.Url);
                return;
            }
            
            await page.ScrollBottom();
            await page.ScrollTop();

            IElementHandle[] catalogueElements = await GetCatalogueElements(page);
            List<(string, string, IReadOnlyList<string>)> catalogueData = [];
            await foreach ((string Id, string Url, IReadOnlyList<string> Photos) data in GetCatalogueItemsMetadata(catalogueElements, page))
                catalogueData.Add(data);

            PaginationEvaluationParserLink evaluationParserLink = PaginationEvaluationParserLink.FromSnapshot(link);
            PaginationEvaluationParserLink processed = evaluationParserLink.MarkProcessed();
            await parsers.UpdateLink(processed, parserSnapshot.Id, ct);
            PaginationEvaluationParserLinkSnapshot processedSnapshot = processed.GetSnapshot();
            
            _logger.Information(
                """
                Processed parser link info:
                ID: {Id}
                Url: {Url}
                Processed: {Processed} 
                """,
                processedSnapshot.Id, processedSnapshot.Url, processedSnapshot.WasProcessed);
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
        IElementHandle[] catalogueItems, IPage page)
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