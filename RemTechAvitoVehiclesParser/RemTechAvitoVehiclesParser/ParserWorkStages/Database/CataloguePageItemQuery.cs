namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed record CataloguePageItemQuery(
    Guid? Id = null,
    bool ProcessedOnly = false,
    bool NotProcessedOnly = false,
    int? RetryLimitTreshold = null,
    bool WithLock = false,
    int? Limit = null);