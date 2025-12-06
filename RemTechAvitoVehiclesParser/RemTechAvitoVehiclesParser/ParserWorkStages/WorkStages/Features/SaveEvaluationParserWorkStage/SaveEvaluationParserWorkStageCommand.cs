namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage;

public sealed record SaveEvaluationParserWorkStageCommand(
    Guid Id,
    string Domain,
    string Type,
    IEnumerable<SaveEvaluationParserWorkLinkArg> Links);