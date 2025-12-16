using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class ConcreteItemParsingProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess ConcreteItems => async (deps, ct) =>
        {
            deps.Deconstruct(
                out BrowserFactory browsers,
                out AvitoBypassFactory bypass,
                out _,
                out Serilog.ILogger dLogger,
                out NpgSqlConnectionFactory npgSql
            );

            Serilog.ILogger logger = dLogger.ForContext<WorkStageProcess>();
            await using NpgSqlSession session = new(npgSql);
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

            IBrowser browser = await browsers.ProvideBrowser(headless: true);
            PendingToPublishItem[] results = await ProcessCatalogueItems(items, logger, browsers, bypass);
            if (!await session.Commited(ct)) logger.Error("Error at committing transaction.");
        };
    }

    private static async Task<PendingToPublishItem[]> ProcessCatalogueItems(
        CataloguePageItem[] items,
        Serilog.ILogger logger,
        BrowserFactory browsers,
        AvitoBypassFactory bypasser)
    {
        IBrowser browser = await browsers.ProvideBrowser(headless: false);
        List<PendingToPublishItem> results = [];

        async Task<IBrowser> Recreate(IBrowser oldBrowser)
        {
            await oldBrowser.DestroyAsync();
            return await browsers.ProvideBrowser(headless: false);
        }

        for (int i = 0; i < items.Length; i++)
        {
            CataloguePageItem target = items[i];

            try
            {
                PendingToPublishItem pendingItem = await target.CreatePendingItem(browser, bypasser);
                results.Add(pendingItem);
                target = target.MarkProcessed();
                logger.Information("Processed item: {Url}", target.Url);
            }
            catch (ArgumentNullException)
            {
                logger.Error("Null reference exception in browser. Recreating browser");
                browser = await Recreate(browser);
            }
            catch (NullReferenceException)
            {
                logger.Error("Null reference exception in browser. Recreating browser");
                browser = await Recreate(browser);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error at extracting concrete item {Url}.", target.Url);
                target = target.IncreaseRetry();
            }
            finally
            {
                items[i] = target;
            }
        }

        return [.. results];
    }
}
