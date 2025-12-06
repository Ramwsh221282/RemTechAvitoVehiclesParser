namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;

public sealed record CataloguePageUrlQuery(
    Guid? Id = null,
    Guid? LinkId = null,
    bool? ProcessedOnly = null,
    bool? UnprocessedOnly = null,
    int? RetryLimitTreshold = null,
    int? RetryLimit = null,
    int? Limit = null,
    bool WithLock = false);