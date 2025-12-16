using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class ConcreteItemParsingProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess ConcreteItems => async (deps, ct) =>
        {
            Serilog.ILogger logger = deps.Logger.ForContext<WorkStageProcess>();
            await using NpgSqlSession session = new(deps.NpgSql);
            await session.UseTransaction(ct);

            WorkStageQuery stageQuery = new(Name: WorkStageConstants.ConcreteItemStageName, WithLock: true);
            Maybe<ParserWorkStage> workStage = await ParserWorkStage.GetSingle(session, stageQuery, ct);
            if (!workStage.HasValue) return;

            CataloguePageItemQuery itemsQuery = new(UnprocessedOnly: true, Limit: 50, RetryCount: 5);
            CataloguePageItem[] items = await CataloguePageItem.GetMany(session, itemsQuery, ct: ct);
            if (items.Length == 0)
            {
                workStage.Value.ToFinalizationStage();
                await workStage.Value.Update(session, ct);
                await session.UnsafeCommit(ct);
                logger.Information("Switched to: {Stage}", workStage.Value.Name);
                return;
            }

            List<PendingToPublishItem> results = [];
            IBrowser browser = await deps.Browsers.ProvideBrowser(headless: true);
            for (int i = 0; i < items.Length; i++)
            {
                CataloguePageItem target = items[i];

                try
                {
                    PendingToPublishItem pending = await target.ExtractPendingItem(
                        browser,
                        deps.Bypasses,
                        ITextTransformer transformer
                        );

                }
                catch (ArgumentNullException)
                {
                    logger.Error("Null reference exception in browser. Recreating browser");
                    await browser.DestroyAsync();
                    browser = await deps.Browsers.ProvideBrowser(headless: false);
                }
                catch (NullReferenceException)
                {
                    logger.Error("Null reference exception in browser. Recreating browser");
                    await browser.DestroyAsync();
                    browser = await deps.Browsers.ProvideBrowser(headless: false);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error at extracting concrete item {Url}.", target.Url);
                }
                finally
                {

                }
            }

            (IEnumerable<CataloguePageItem> catalogueItems, IEnumerable<PendingToPublishItem> results) = await ProcessItems(items, deps);

            await itemsStorage.UpdateMany(catalogueItems);
            await npgSqlPendingItemsStorage.SaveMany(results);

            if (!await session.Commited(ct)) logger.Error("Error at committing transaction.");
        };
    }

    private static async Task<(IEnumerable<CataloguePageItem>, IEnumerable<PendingToPublishItem>)> ProcessItems(
        CataloguePageItem[] items,
        WorkStageProcessDependencies deps)
    {
        Serilog.ILogger logger = deps.Logger.ForContext<WorkStageProcess>();
        ITextTransformer transformer = deps.TextTransformerBuilder
            .UsePunctuationCleaner()
            .UseEmojiCleaner()
            .UseNewLinesCleaner()
            .UseSpacesCleaner()
            .Build();


        IBrowser browser = await deps.Browsers.ProvideBrowser(headless: false);
        List<CataloguePageItem> processed = [];
        List<PendingToPublishItem> pendingItems = [];
        foreach (CataloguePageItem item in items)
        {
            string url = item.ReadUrl();
            IReadOnlyList<string> photos = item.ReadPhotos();

            try
            {
                logger.Information("Processing concrete item: {Url}", url);
                var advertisement = await AvitoSpecialEquipmentAdvertisement.Create(await browser.GetPage(), url, deps.Bypasses);
                (CataloguePageItem result, bool success) = await ResolveByPropertiesExistance(transformer, item, advertisement);
                if (success)
                {
                    AvitoSpecialEquipmentAdvertisementSnapshot advertisementSnapshot = advertisement.GetSnapshot();
                    PendingToPublishItem pendingItem = CreatePending(item.Id, url, photos, advertisementSnapshot);
                    pendingItems.Add(pendingItem);
                }


                processed.Add(result);
            }
            catch (ArgumentNullException)
            {
                logger.Error("Null reference exception in browser. Recreating browser");
                await browser.DestroyAsync();
                browser = await deps.Browsers.ProvideBrowser(headless: false);
            }
            catch (NullReferenceException)
            {
                logger.Error("Null reference exception in browser. Recreating browser");
                await browser.DestroyAsync();
                browser = await deps.Browsers.ProvideBrowser(headless: false);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to process advertisement. URL: {Url}", url);
                CataloguePageItem result = item.IncreaseRetry();
                processed.Add(result);
            }
        }

        await browser.DestroyAsync();
        return (processed, pendingItems);
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
}
