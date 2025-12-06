namespace RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage;

public sealed record SaveEvaluationParserWorkStageCommand(
    Guid Id,
    string Domain,
    string Type,
    IEnumerable<SaveEvaluationParserWorkLinkArg> Links);