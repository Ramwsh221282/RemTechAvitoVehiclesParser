using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed record PaginationEvaluationParserSnapshot(
    Guid Id,
    string Domain,
    string Type,
    IReadOnlyList<PaginationEvaluationParserLinkSnapshot> Links) 
    : ISnapshot;