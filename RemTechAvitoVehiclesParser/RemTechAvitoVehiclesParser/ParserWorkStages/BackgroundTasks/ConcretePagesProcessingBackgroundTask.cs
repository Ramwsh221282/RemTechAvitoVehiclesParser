using System.Text.Json;
using PuppeteerSharp;
using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.Utilities.TextTransforming;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class ConcretePagesProcessingBackgroundTask(
    NpgSqlDataSourceFactory dataSourceFactory,
    Serilog.ILogger logger,
    BrowserFactory browserFactory,
    AvitoBypassFactory bypassFactory,
    TextTransformerBuilder textTransformerBuilder
    ) :
    ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ConcretePagesProcessingBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using IPostgreSqlAdapter adapter = await dataSourceFactory.CreateAdapter(ct);
        NpgSqlParserWorkStagesStorage stagesStorage = new(adapter);
        NpgSqlCataloguePageItemsStorage itemsStorage = new(adapter);

        await adapter.UseTransaction(ct);
        ParserWorkStageQuery stageQuery = new(Name: WorkStageConstants.ConcreteItemStageName, WithLock: true);
        Maybe<ParserWorkStage> workStage = await stagesStorage.GetWorkStage(stageQuery, ct);
        if (!workStage.HasValue) return;

        CataloguePageItemQuery itemsQuery = new(NotProcessedOnly: true, Limit: 50, RetryLimitTreshold: 5);
        CataloguePageItem[] items = [..await itemsStorage.GetItems(itemsQuery, ct)];
        if (items.Length == 0) return; // probably should stop stage and switch to final.
        
        CataloguePageItemSnapshot[] snapshots = [..items.Select(i => i.GetSnapshot())];
        IEnumerable<CataloguePageItem> processed = await ProcessItems(snapshots);
        await itemsStorage.UpdateMany(processed);
        
        if (!await adapter.TransactionCommited(ct)) _logger.Error("Error at committing transaction.");
    }

    private async Task<IEnumerable<CataloguePageItem>> ProcessItems(CataloguePageItemSnapshot[] snapshots)
    {
        ITextTransformer transformer = textTransformerBuilder
            .UsePunctuationCleaner()
            .UseNewLinesCleaner()
            .UseSpacesCleaner()
            .Build();
        
        IBrowser browser = await browserFactory.ProvideBrowser(headless: false);
        List<CataloguePageItem> processed = [];
        foreach (CataloguePageItemSnapshot itemSnapshot in snapshots)
        {
            CataloguePageItem fromSnapshot = CataloguePageItem.FromSnapshot(itemSnapshot);
            using JsonDocument document = JsonDocument.Parse(itemSnapshot.Payload);
            string url = document.RootElement.GetProperty("url").GetString()!;
            IReadOnlyList<string> photos = GetPhotosFromJson(document);
            try
            {
                _logger.Information("Processing concrete item: {Url}", url);
                AvitoSpecialEquipmentAdvertisement advertisement =
                    await AvitoSpecialEquipmentAdvertisement.Create(await browser.GetPage(), url, bypassFactory);

                bool[] propertiesExistance =
                [
                    await advertisement.HasTitle(),
                    await advertisement.HasPrice(),
                    await advertisement.HasCharacteristics(),
                    await advertisement.HasDescription(transformer),
                    await advertisement.HasAddress(transformer)
                ];

                CataloguePageItem result = propertiesExistance.All(i => i)
                    ? fromSnapshot.MarkProcessed()
                    : fromSnapshot.IncreaseRetry();

                processed.Add(result);
            }
            catch(NullReferenceException)
            {
                _logger.Error("Null reference exception in browser. Recreating browser");
                await browser.DestroyAsync();
                browser = await browserFactory.ProvideBrowser(headless: false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to process advertisement. URL: {Url}", url);
                CataloguePageItem result = fromSnapshot.IncreaseRetry();
                processed.Add(result);
            }
        }
        
        await browser.DestroyAsync();
        return processed;
    }
    
    private static IReadOnlyList<string> GetPhotosFromJson(JsonDocument document)
    {
        List<string> photos = [];
        foreach (JsonElement photoJson in document.RootElement.GetProperty("photos").EnumerateArray())
            photos.Add(photoJson.GetString()!);
        return photos;
    }
}