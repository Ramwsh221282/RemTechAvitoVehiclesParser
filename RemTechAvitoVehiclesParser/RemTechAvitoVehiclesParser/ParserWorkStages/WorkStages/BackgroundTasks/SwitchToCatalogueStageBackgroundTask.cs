using ParsingSDK.Parsing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class SwitchToCatalogueStageBackgroundTask(Serilog.ILogger logger, NpgSqlConnectionFactory npgSql) : ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<SwitchToCatalogueStageBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            CancellationToken ct = context.CancellationToken;
            await using NpgSqlSession session = new(npgSql);
            await session.UseTransaction(ct);
            NpgSqlPaginationParsingParsersStorage parsersStorage = new(session);
            NpgSqlParserWorkStagesStorage stages = new(session);
            NpgSqlCataloguePageUrlsStorage catalogueUrls = new(session);
            WorkStageQuery stageQuery = new(WithLock: true, Name: WorkStageConstants.EvaluationStageName);
            Maybe<ParserWorkStage> stage = await stages.GetWorkStage(stageQuery, ct);
            if (!stage.HasValue) return;

            PaginationEvaluationParsersQuery parserQuery = new(
                LinksWithCurrentPage: true,
                LinksWithMaxPage: true,
                ParserId: stage.Value.Id, 
                WithLock: true);
            
            Maybe<PaginationParsingParser> parser = await parsersStorage.GetParser(parserQuery, ct);
            if (!parser.HasValue) return;
            if (!parser.Value.AllLinksHavePagesInitialized()) return;

            foreach (PaginationParsingParserLink link in parser.Value.Links)
            {
                CataloguePageUrl[] urls = link.BuildCataloguePageUrls();
                await catalogueUrls.SaveMany(urls);
                _logger.Information("Saved: {Count} catalogue urls.", urls.Length);
            }
            
            ParserWorkStage switched = stage.Value.ChangeStage(new CatalogueWorkStage(stage.Value));
            await stages.Update(switched, ct);
            await session.UnsafeCommit(ct);
            
            _logger.Information(
                """
                Parser work stage switched info:
                ID {Id}
                Stage: {Name}
                Finished: {Finished}
                """, switched.Id, switched.Name, switched.Finished.HasValue);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check if can switch to catalogue stage.");
        }
    }
}