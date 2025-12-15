using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage.Decorators;

public sealed class SaveEvaluationParserWorkStageTransaction(
    NpgSqlSession session,
    ISaveEvaluationParserWorkStage origin
) : ISaveEvaluationParserWorkStage
{
    public async Task<(ParserWorkStage stage, ProcessingParser parser)> Handle(
        SaveEvaluationParserWorkStageCommand command,
        CancellationToken ct = default
    )
    {
        await session.UseTransaction();
        (ParserWorkStage stage, ProcessingParser parser) result = await origin.Handle(command, ct);
        await session.UnsafeCommit(ct);
        return result;
    }
}
