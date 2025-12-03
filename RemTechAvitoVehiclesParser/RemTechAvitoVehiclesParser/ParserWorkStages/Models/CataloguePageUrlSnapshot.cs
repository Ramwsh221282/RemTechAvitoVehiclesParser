using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed record CataloguePageUrlSnapshot(
    Guid Id, 
    Guid LinkId, 
    string Url, 
    bool Processed,
    int RetryCount) : ISnapshot;