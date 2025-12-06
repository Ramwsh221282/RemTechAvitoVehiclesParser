namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed record CataloguePageItemsQuery(
    Guid? Id = null,
    Guid? CatalogueUrlId = null,
    bool ProcessedOnly = false,
    bool NotProcessedOnly = false,
    int? Limit = null,
    int? RetryTreshold = null);