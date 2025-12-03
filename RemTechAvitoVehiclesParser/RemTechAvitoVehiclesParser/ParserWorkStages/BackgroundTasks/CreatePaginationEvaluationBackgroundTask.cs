using PuppeteerSharp;
using Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.BackgroundTasks;

// public sealed class CreatePaginationEvaluationBackgroundTask(
//     Serilog.ILogger logger,
//     NpgSqlSession session,
//     BrowserFactory factory
//     ) : 
//     ICronScheduleJob
// {
//     private readonly NpgSqlCataloguePaginationStorage _paginations = new(session);
//     private readonly NpgSqlParserWorkStagesStorage _workStages = new(session);
//     private readonly Serilog.ILogger _logger = logger.ForContext<CreatePaginationEvaluationBackgroundTask>();
//     
//     public async Task Execute(IJobExecutionContext context)
//     {
//         CancellationToken ct = context.CancellationToken;
//         _logger.Information("Starting create pagination evaluation background job.");
//
//         await session.UseTransaction(ct);
//         await using (session)
//         {
//             Maybe<ParserWorkStage> evaluationStage = await _workStages.GetWorkStage(
//                 new ParserWorkStageQuery(Name: WorkStageConstants.EvaluationStageName), 
//                 ct);
//             
//             if (!evaluationStage.HasValue)
//             {
//                 _logger.Information("No evaluation work stage exists. Stopping job.");
//                 return;
//             }
//             
//             _logger.Information("Evaluation work stage detected. Starting evaluating pagination.");
//
//             await using (IBrowser browser = await factory.ProvideBrowser(headless: false))
//             {
//                 
//             }
//         }
//     }
// }