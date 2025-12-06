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
        NpgSqlPendingToPublishItemsStorage npgSqlPendingItemsStorage = new(adapter);

        await adapter.UseTransaction(ct);
        ParserWorkStageQuery stageQuery = new(Name: WorkStageConstants.ConcreteItemStageName, WithLock: true);
        Maybe<ParserWorkStage> workStage = await stagesStorage.GetWorkStage(stageQuery, ct);
        if (!workStage.HasValue) return;

        CataloguePageItemQuery itemsQuery = new(NotProcessedOnly: true, Limit: 50, RetryLimitTreshold: 5);
        CataloguePageItem[] items = [..await itemsStorage.GetItems(itemsQuery, ct)];
        if (items.Length == 0)
        {
            await SwitchStageToFinalization(workStage.Value, stagesStorage, ct);
            if (!await adapter.TransactionCommited(ct)) _logger.Error("Error at committing transaction.");
        }
        
        CataloguePageItemSnapshot[] snapshots = [..items.Select(i => i.GetSnapshot())];
        
        (IEnumerable<CataloguePageItem> catalogueItems, IEnumerable<PendingToPublishItem> results) = await ProcessItems(snapshots);
        
        int updated = await itemsStorage.UpdateMany(catalogueItems);
        int inserted = await npgSqlPendingItemsStorage.SaveMany(results);
        
        _logger.Information("""
                            Concrete items processing info: 
                            Updated catalogue items: {CatLength}
                            Saved pending items: {PendLength}
                            """, updated, inserted);
        
        if (!await adapter.TransactionCommited(ct)) _logger.Error("Error at committing transaction.");
    }

    private async Task<(IEnumerable<CataloguePageItem>, IEnumerable<PendingToPublishItem>)> ProcessItems(CataloguePageItemSnapshot[] snapshots)
    {
        ITextTransformer transformer = textTransformerBuilder
            .UsePunctuationCleaner()
            .UseEmojiCleaner()
            .UseNewLinesCleaner()
            .UseSpacesCleaner()
            .Build();
        
        IBrowser browser = await browserFactory.ProvideBrowser(headless: false);
        List<CataloguePageItem> processed = [];
        List<PendingToPublishItem> pendingItems = [];
        foreach (CataloguePageItemSnapshot itemSnapshot in snapshots)
        {
            CataloguePageItem fromSnapshot = CataloguePageItem.FromSnapshot(itemSnapshot);
            string url = itemSnapshot.GetUrl();
            IReadOnlyList<string> photos = itemSnapshot.GetPhotos();

            try
            {
                processed.Add(fromSnapshot.MarkProcessed());
                _logger.Information("Processing concrete item: {Url}", url);
                var advertisement = await AvitoSpecialEquipmentAdvertisement.Create(await browser.GetPage(), url, bypassFactory);
                (CataloguePageItem result, bool success) = await ResolveByPropertiesExistance(transformer, fromSnapshot, advertisement);
                if (success)
                {
                    AvitoSpecialEquipmentAdvertisementSnapshot advertisementSnapshot = advertisement.GetSnapshot();
                    PendingToPublishItem pendingItem = CreatePending(itemSnapshot.Id, url, photos, advertisementSnapshot);
                    pendingItems.Add(pendingItem);
                }
                
                processed.Add(result);
            }
            catch(ArgumentNullException)
            {
                _logger.Error("Null reference exception in browser. Recreating browser");
                await browser.DestroyAsync();
                browser = await browserFactory.ProvideBrowser(headless: false);
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
        return (processed, pendingItems);
    }

    private async Task SwitchStageToFinalization(ParserWorkStage stage, NpgSqlParserWorkStagesStorage storage, CancellationToken ct)
    {
        ParserWorkStage finalStage = stage.ChangeStage(new ParserWorkStage.FinalizationWorkStage(stage));
        await storage.Update(finalStage, ct);
    }

    private static async Task<(CataloguePageItem, bool)> ResolveByPropertiesExistance(
        ITextTransformer transformer,
        CataloguePageItem item,
        AvitoSpecialEquipmentAdvertisement advertisement)
    {
        bool hasTitle = await advertisement.HasTitle();
        if (!hasTitle) return (item.IncreaseRetry(), false);
        bool hasPrice = await advertisement.HasPrice();
        if (!hasPrice) return (item.IncreaseRetry(), false);
        bool hasCharacteristics = await advertisement.HasCharacteristics();
        if (!hasCharacteristics) return (item.IncreaseRetry(), false);
        bool hasDescription = await advertisement.HasDescription(transformer);
        if (!hasDescription) return (item.IncreaseRetry(), false);
        bool hasAddress = await advertisement.HasAddress(transformer);
        if (!hasAddress) return (item.IncreaseRetry(), false);
        return (item.MarkProcessed(), true);
    }
    
    private static PendingToPublishItem CreatePending(
        string id, 
        string url,
        IEnumerable<string> photos,
        AvitoSpecialEquipmentAdvertisementSnapshot advertisement
        )
    {
        return new(
            id: id,
            url: url,
            title: advertisement.Title,
            address: advertisement.Address,
            price: advertisement.Price,
            isNds: advertisement.IsNds,
            descriptionList: advertisement.DescriptionList,
            characteristicList: advertisement.Characteristics,
            photos: photos,
            wasProcessed: false
        );
    }
}