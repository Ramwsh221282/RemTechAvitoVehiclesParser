namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed record CataloguePageUrlQuery(
    Guid? Id = null,
    Guid? LinkId = null,
    bool? ProcessedOnly = null,
    int? RetryLimit = null,
    int? Limit = null);