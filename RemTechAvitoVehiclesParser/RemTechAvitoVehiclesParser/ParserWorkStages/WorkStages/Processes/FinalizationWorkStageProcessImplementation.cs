using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.Common;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class FinalizationWorkStageProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess Finalization => async (deps, ct) =>
        {
             deps.Deconstruct(
                 out _, 
                 out _, 
                 out TextTransformerBuilder textTransformerBuilder, 
                 out Serilog.ILogger dLogger, 
                 out NpgSqlConnectionFactory npgSql);

            Serilog.ILogger logger = dLogger.ForContext<WorkStageProcess>();
            await using NpgSqlSession session = new(npgSql);
            await session.UseTransaction(ct);

            WorkStageQuery stageQuery = new(Name: WorkStageConstants.FinalizationStage, WithLock: true);
            Maybe<ParserWorkStage> stage = await ParserWorkStage.GetSingle(session, stageQuery, ct);
            if (!stage.HasValue) return;

            AvitoItemQuery itemsQuery = new(ConcreteOnly: true, WithLock: true, Limit: 50);
            AvitoVehicle[] items = await AvitoVehicle.GetAsConcreteRepresentation(session, itemsQuery, ct);
            
            if (items.Length == 0)
            {
                stage.Value.ToSleepingStage();
                await stage.Value.Update(session, ct);
                await session.UnsafeCommit(ct);
                logger.Information("Switched to sleeping work stage.");
                return;
            }

            await items.Remove(session);
            await session.UnsafeCommit(ct);
            logger.Information("Finalized {Count} items.", items.Length);
        };
    }
}