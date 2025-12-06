using ParsingSDK.Parsing;
using PuppeteerSharp;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.Utilities.TextTransforming;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class ConcretePagesProcessingBackgroundTask(
    NpgSqlConnectionFactory npgSlqConnectionFactory,
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
        await using NpgSqlSession session = new(npgSlqConnectionFactory);
        NpgSqlParserWorkStagesStorage stagesStorage = new(session);
        NpgSqlCataloguePageItemsStorage itemsStorage = new(session);
        NpgSqlPendingToPublishItemsStorage npgSqlPendingItemsStorage = new(session);

        await session.UseTransaction(ct);
        WorkStageQuery stageQuery = new(Name: WorkStageConstants.ConcreteItemStageName, WithLock: true);
        Maybe<ParserWorkStage> workStage = await stagesStorage.GetWorkStage(stageQuery, ct);
        if (!workStage.HasValue) return;

        CataloguePageItemQuery itemsQuery = new(NotProcessedOnly: true, Limit: 50, RetryLimitTreshold: 5);
        CataloguePageItem[] items = [..await itemsStorage.GetItems(itemsQuery, ct)];
        if (items.Length == 0)
        {
            await SwitchStageToFinalization(workStage.Value, stagesStorage, ct);
            if (!await session.Commited(ct)) _logger.Error("Error at committing transaction.");
            return;
        }
        
        (IEnumerable<CataloguePageItem> catalogueItems, IEnumerable<PendingToPublishItem> results) = await ProcessItems(items);
        
        await itemsStorage.UpdateMany(catalogueItems);
        await npgSqlPendingItemsStorage.SaveMany(results);
        
        if (!await session.Commited(ct)) _logger.Error("Error at committing transaction.");
    }

    private async Task<(IEnumerable<CataloguePageItem>, IEnumerable<PendingToPublishItem>)> ProcessItems(CataloguePageItem[] items)
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
        foreach (CataloguePageItem item in items)
        {
            string url = item.ReadUrl();
            IReadOnlyList<string> photos = item.ReadPhotos();

            try
            {
                _logger.Information("Processing concrete item: {Url}", url);
                var advertisement = await AvitoSpecialEquipmentAdvertisement.Create(await browser.GetPage(), url, bypassFactory);
                (CataloguePageItem result, bool success) = await ResolveByPropertiesExistance(transformer, item, advertisement);
                if (success)
                {
                    AvitoSpecialEquipmentAdvertisementSnapshot advertisementSnapshot = advertisement.GetSnapshot();
                    PendingToPublishItem pendingItem = CreatePending(item.Id, url, photos, advertisementSnapshot);
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
                CataloguePageItem result = item.IncreaseRetry();
                processed.Add(result);
            }
        }
        
        await browser.DestroyAsync();
        return (processed, pendingItems);
    }

    private async Task SwitchStageToFinalization(ParserWorkStage stage, NpgSqlParserWorkStagesStorage storage, CancellationToken ct)
    {
        ParserWorkStage finalStage = stage.ChangeStage(new FinalizationWorkStage(stage));
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
            Id: id,
            Url: url,
            Title: advertisement.Title,
            Address: advertisement.Address,
            Price: advertisement.Price,
            IsNds: advertisement.IsNds,
            DescriptionList: advertisement.DescriptionList,
            Characteristics: advertisement.Characteristics,
            Photos: [..photos],
            WasProcessed: false
        );
    }
}