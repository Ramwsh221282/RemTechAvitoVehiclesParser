using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage;

public sealed class SaveEvaluationParserWorkStage(
    NpgSqlParserWorkStagesStorage storage
) : ISaveEvaluationParserWorkStage
{
    public async Task<ParserWorkStageSnapshot> Handle(SaveEvaluationParserWorkStageCommand command, CancellationToken ct = default)
    {
        ParserWorkStage stage = new ParserWorkStage.EvaluationWorkStage(command.Id);
        await storage.Save(stage, ct);
        return stage.GetSnapshot();
    }
}