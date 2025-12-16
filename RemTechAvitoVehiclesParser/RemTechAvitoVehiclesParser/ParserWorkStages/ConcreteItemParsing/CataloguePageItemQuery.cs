namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing;

public sealed record CataloguePageItemQuery(
    bool UnprocessedOnly = false,     
    bool WithLock = false,
    int? Limit = null,
    int? RetryCount = null
);
