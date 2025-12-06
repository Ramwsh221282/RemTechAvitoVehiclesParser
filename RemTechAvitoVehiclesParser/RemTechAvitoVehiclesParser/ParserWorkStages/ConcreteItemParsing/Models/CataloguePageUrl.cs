namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

public sealed record CataloguePageUrl(
    Guid Id,
    Guid LinkId,
    string Url,
    bool Processed,
    int RetryCount,
    IReadOnlyList<CataloguePageItem> Items);