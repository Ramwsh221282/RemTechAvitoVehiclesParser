namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing;

public sealed record CataloguePageItem(
    string Id,
    string Url,
    string Payload,
    bool WasProcessed,
    int RetryCount);