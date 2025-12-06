namespace RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;

public sealed record PendingToPublishItem(
    string Id,
    string Url,
    string Title,
    string Address,
    long Price,
    bool IsNds,
    IReadOnlyList<string> DescriptionList,
    IReadOnlyList<string> Characteristics,
    IReadOnlyList<string> Photos,
    bool WasProcessed);