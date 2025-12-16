using ParsingSDK.Parsing;
using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class WorkStageProcessInvoker(WorkStageProcessDependencies deps) : ICronScheduleJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        Maybe<ParserWorkStage> workStage = await GetWorkStage(context.CancellationToken);
        if (!workStage.HasValue) return;

        WorkStageProcess process = WorkStageProcessRouter.Route(workStage.Value);
        await process(deps, context.CancellationToken);
    }

    private async Task<Maybe<ParserWorkStage>> GetWorkStage(CancellationToken ct)
    {
        WorkStageQuery query = new();
        await using NpgSqlSession session = new(deps.NpgSql);
        Maybe<ParserWorkStage> workStage = await ParserWorkStage.GetSingle(session, query, ct);
        return workStage;
    }
}
