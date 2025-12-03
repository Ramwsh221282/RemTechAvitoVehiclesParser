using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed record PaginationEvaluationParserLinkSnapshot(
    Guid Id, 
    string Url,
    bool WasProcessed,
    int? CurrentPage,
    int? MaxPage) : ISnapshot;