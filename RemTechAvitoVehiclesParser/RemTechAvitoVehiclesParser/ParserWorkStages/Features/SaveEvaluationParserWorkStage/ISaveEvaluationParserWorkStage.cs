using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage;

public interface ISaveEvaluationParserWorkStage
{
    Task<(ParserWorkStageSnapshot stage, PaginationEvaluationParserSnapshot parser)> Handle(
        SaveEvaluationParserWorkStageCommand command, 
        CancellationToken ct = default);
}