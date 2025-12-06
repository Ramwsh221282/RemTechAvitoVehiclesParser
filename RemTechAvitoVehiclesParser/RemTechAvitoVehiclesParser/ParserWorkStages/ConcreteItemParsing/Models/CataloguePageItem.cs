namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

public sealed record CataloguePageItem(
    string Id,
    Guid CatalogueUrlId,
    string Payload,
    bool WasProcessed,
    int RetryCount);