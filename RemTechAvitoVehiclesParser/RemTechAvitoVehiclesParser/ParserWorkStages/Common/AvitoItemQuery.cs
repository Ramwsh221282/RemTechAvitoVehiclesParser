namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common;

public sealed record AvitoItemQuery(
    bool UnprocessedOnly = false,     
    bool WithLock = false,
    int? Limit = null,
    int? RetryCount = null,
    bool CatalogueOnly = false,
    bool ConcreteOnly = false
);
