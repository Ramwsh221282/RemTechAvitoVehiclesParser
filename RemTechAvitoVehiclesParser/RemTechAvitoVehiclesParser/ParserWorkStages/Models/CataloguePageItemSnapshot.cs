using System.Text.Json;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed record CataloguePageItemSnapshot(
    string Id,
    Guid CatalogueUrlId,
    string Payload,
    bool WasProcessed,
    int RetryCount
) : ISnapshot
{
    public string GetUrl()
    {
        using JsonDocument document = JsonDocument.Parse(Payload);
        return document.RootElement.GetProperty("url").GetString()!;
    }

    public IReadOnlyList<string> GetPhotos()
    {
        using JsonDocument document = JsonDocument.Parse(Payload);
        List<string> photos = [];
        foreach (JsonElement photoJson in document.RootElement.GetProperty("photos").EnumerateArray())
            photos.Add(photoJson.GetString()!);
        return photos;
    }
}