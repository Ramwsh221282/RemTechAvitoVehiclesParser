using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage;

public interface ISaveEvaluationParserWorkStage
{
    Task<(ParserWorkStage stage, PaginationParsingParser parser)> Handle(SaveEvaluationParserWorkStageCommand command, CancellationToken ct = default);
}