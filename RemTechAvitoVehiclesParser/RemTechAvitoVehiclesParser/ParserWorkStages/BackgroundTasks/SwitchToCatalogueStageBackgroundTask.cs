using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class SwitchToCatalogueStageBackgroundTask(
    Serilog.ILogger logger,
    NpgSqlDataSourceFactory dataSourceFactory
    ) : ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<SwitchToCatalogueStageBackgroundTask>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            CancellationToken ct = context.CancellationToken;
            await using IPostgreSqlAdapter session = await dataSourceFactory.CreateAdapter(context.CancellationToken);
            await session.UseTransaction(ct);
            NpgSqlPaginationEvaluationParsersStorage parsers = new(session);
            NpgSqlParserWorkStagesStorage stages = new(session);
            NpgSqlCataloguePageUrlsStorage catalogueUrls = new(session);
            ParserWorkStageQuery stageQuery = new(WithLock: true, Name: WorkStageConstants.EvaluationStageName);
            Maybe<ParserWorkStage> stage = await stages.GetWorkStage(stageQuery, ct);
            if (!stage.HasValue) return;

            PaginationEvaluationParsersQuery parserQuery = new(
                LinksWithCurrentPage: true,
                LinksWithMaxPage: true,
                ParserId: stage.Value.GetSnapshot().Id, 
                WithLock: true);
            
            Maybe<PaginationEvaluationParser> parser = await parsers.GetParser(parserQuery, ct);
            if (!parser.HasValue) return;
            if (!parser.Value.GetSnapshot().Links.All(l => l.CurrentPage.HasValue && l.MaxPage.HasValue)) return;

            foreach (PaginationEvaluationParserLink link in parser.Value.Links())
            {
                CataloguePageUrl[] urls = link.BuildCataloguePageUrls();
                int saved = await catalogueUrls.SaveMany(urls);
                _logger.Information("Saved: {Count} catalogue urls.", saved);
            }
            
            ParserWorkStage switched = stage.Value.ChangeStage(new ParserWorkStage.CatalogueWorkStage(stage.Value));
            await stages.Update(switched, ct);
            await session.CommitTransaction(ct);
            
            ParserWorkStageSnapshot snapshot = switched.GetSnapshot();
            
            _logger.Information(
                """
                Parser work stage switched info:
                ID {Id}
                Stage: {Name}
                Finished: {Finished}
                """, snapshot.Id, snapshot.Name, snapshot.Finished.HasValue);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check if can switch to catalogue stage.");
        }
    }
}