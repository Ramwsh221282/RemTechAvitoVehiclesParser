namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;

public record CataloguePageUrlQuery(
    bool ProcessedOnly = false,
    bool UnprocessedOnly = false,
    int? RetryLimit = null,
    bool WithLock = false,
    int? Limit = null
);
