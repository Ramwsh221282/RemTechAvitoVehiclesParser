using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;
using AvitoFirewallBypass;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class CataloguePagesParsingProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess CatalogueProcess =>
            async (deps, ct) =>
            {
                deps.Deconstruct(
                    out BrowserFactory browsers,
                    out AvitoBypassFactory bypasses,
                    out _,
                    out Serilog.ILogger dLogger,
                    out NpgSqlConnectionFactory npgSql
                );

                Serilog.ILogger logger = dLogger.ForContext<WorkStageProcess>();
                await using NpgSqlSession session = new(npgSql);
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
                if (!stage.HasValue) return;

                CataloguePageUrlQuery pageUrlQuery = new(
                    UnprocessedOnly: true,
                    RetryLimit: 5,
                    WithLock: true,
                    Limit: 20
                );

                CataloguePageUrl[] urls = await CataloguePageUrl.GetMany(session, pageUrlQuery, ct);
                if (urls.Length == 0)
                {
                    stage.Value.ToConcreteStage();
                    await stage.Value.Update(session, ct);
                    await session.UnsafeCommit(ct);
                    logger.Information("Switched to stage: {Stage}", stage.Value.Name);
                    return;
                }

                IBrowser browser = await deps.Browsers.ProvideBrowser(headless: false);

                for (int i = 0; i < urls.Length; i++)
                {
                    CataloguePageUrl url = urls[i];
                    logger.Information("Processing page: {Url}", url.Url);

                    try
                    {
                        CataloguePageItem[] items = await url.ExtractPageItems(browser, deps.Bypasses);
                        url = url.MarkProcessed();
                        await items.PersistMany(session);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error at processing: {Url}", url);
                        url = url.IncrementRetryCount();
                    }
                    finally
                    {
                        urls[i] = url;
                    }
                }

                await browser.DestroyAsync();
                await urls.UpdateMany(session);

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
}
