using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage.Decorators;

public sealed class SaveEvaluationParserWorkStageTransaction(
    IPostgreSqlAdapter session,
    ISaveEvaluationParserWorkStage origin
) : ISaveEvaluationParserWorkStage
{
    public async Task<(ParserWorkStageSnapshot stage, PaginationEvaluationParserSnapshot parser)> Handle(
        SaveEvaluationParserWorkStageCommand command, 
        CancellationToken ct = default)
    {
        await session.UseTransaction();
        (ParserWorkStageSnapshot stage, PaginationEvaluationParserSnapshot parser) result = await origin.Handle(command, ct);
        await session.CommitTransaction();
        return result;
    }
}