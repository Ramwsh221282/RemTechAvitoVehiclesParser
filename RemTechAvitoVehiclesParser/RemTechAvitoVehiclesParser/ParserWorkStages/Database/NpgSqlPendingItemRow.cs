using System.Text.Json;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed class NpgSqlPendingItemRow
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Address { get; init; }
    public required long Price { get; init; }
    public required bool IsNds { get; init; }
    public required string DescriptionList { get; init; }
    public required string Characteristics { get; init; }
    public required bool WasProcessed { get; init; }
    public required string Photos { get; init; }

    public PendingToPublishItem PendingItem()
    {
        return new PendingToPublishItem(
            id: Id,
            url: Url,
            title: Title,
            address: Address,
            price: Price,
            isNds: IsNds,
            descriptionList: ReadStringJsonArray(DescriptionList),
            characteristicList: ReadStringJsonArray(Characteristics),
            photos: ReadStringJsonArray(Photos),
            WasProcessed);
    }

    private IReadOnlyList<string> ReadStringJsonArray(string jsonArray)
    {
        using JsonDocument document = JsonDocument.Parse(jsonArray);
        List<string> items = new List<string>(document.RootElement.GetArrayLength());
        foreach (JsonElement item in document.RootElement.EnumerateArray())
            items.Add(item.GetString()!);
        return items;
    }
}