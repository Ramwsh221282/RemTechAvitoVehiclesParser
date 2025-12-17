using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.CreateCataloguePageUrls;
using RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class PaginationExtractionProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess PaginationExtraction =>
            async (deps, ct) =>
            {
                Serilog.ILogger logger = deps.Logger.ForContext<WorkStageProcess>();
                await using NpgSqlSession session = new(deps.NpgSql);
                await session.UseTransaction(ct);
                WorkStageQuery stageQuery = new(Name: WorkStageConstants.EvaluationStageName, WithLock: true);
                Maybe<ParserWorkStage> evalStage = await ParserWorkStage.GetSingle(session, stageQuery, ct);
                if (!evalStage.HasValue)
                    return;

                ProcessingParserLinkQuery linksQuery = new(UnprocessedOnly: true, RetryLimit: 10, WithLock: true);
                ProcessingParserLink[] links = await ProcessingParserLink.GetMany(session, linksQuery, ct: ct);
                if (links.Length == 0)
                {
                    evalStage.Value.ToCatalogueStage();
                    await evalStage.Value.Update(session, ct);
                    await session.UnsafeCommit(ct);
                    logger.Information("Switched to stage: {Name}", evalStage.Value.Name);
                    return;
                }

                logger.Information("Starting extracting pagination for links.");
                IBrowser browser = await deps.Browsers.ProvideBrowser();

                for (int i = 0; i < links.Length; i++)
                {
                    ProcessingParserLink link = links[i];
                    try
                    {
                        CataloguePageUrl[] pagedUrls = await new CreateCataloguePageUrlsCommand(() => browser.GetPage(), link.Url, deps.Bypasses)
                            .UseLogging(deps.Logger)
                            .Handle();
                        
                        await pagedUrls.PersistMany(session);
                        link = link.MarkProcessed();
                    }
                    catch (EvaluationFailedException)
                    {
                        browser = await deps.Browsers.Recreate(browser); 
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error at extracting pagination for: {Url}", link.Url);
                        link = link.IncreaseRetryCount();
                    }
                    finally
                    {
                        links[i] = link;
                    }
                }

                await browser.DestroyAsync();
                await links.UpdateMany(session);

                try
                {
                    await session.UnsafeCommit(ct);
                    logger.Information("Committed transaction.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Could not commit transaction");
                }

                logger.Information("Pagination extracting finished.");
            };
    }
}
