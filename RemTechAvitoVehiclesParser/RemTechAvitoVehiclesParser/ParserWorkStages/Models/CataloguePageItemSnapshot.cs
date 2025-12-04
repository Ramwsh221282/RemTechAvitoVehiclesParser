using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed record CataloguePageItemSnapshot(
    string Id,
    Guid CatalogueUrlId,
    string Payload,
    bool WasProcessed,
    int RetryCount
) : ISnapshot;