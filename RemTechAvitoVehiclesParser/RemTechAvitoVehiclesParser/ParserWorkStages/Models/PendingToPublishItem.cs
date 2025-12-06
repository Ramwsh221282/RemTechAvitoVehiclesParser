using RemTechAvitoVehiclesParser.ResultsPublishing;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed class PendingToPublishItem : ISnapshotSource<PendingToPublishItem, PendingToPublishItemSnapshot>
{
    private string Id { get; init; }
    private string Url { get; init; }
    private string Title { get; init; }
    private string Address { get; init; }
    private long Price { get; init; }
    private bool IsNds { get; init; }
    private IReadOnlyList<string> DescriptionList { get; init; }
    private IReadOnlyList<string> Characteristics { get; init; }
    private IReadOnlyList<string> Photos { get; init; }
    private bool WasProcessed { get; init; }

    public PendingToPublishItem MarkProcessed()
    {
        if (WasProcessed)
            throw new InvalidOperationException(
                """
                Cannot mark as processed.
                Item is already processed.
                """);
        return new PendingToPublishItem(this) { WasProcessed = true };
    }
    
    public PendingToPublishItemSnapshot GetSnapshot() => new()
    {
        Id = Id,
        Url = Url,
        Title = Title,
        Address = Address,
        Price = Price,
        IsNds = IsNds,
        DescriptionList = DescriptionList,
        Characteristics = Characteristics,
        WasProcessed = WasProcessed,
        Photos = Photos
    };
    
    public PendingToPublishItem(
        string id,
        string url,
        string title,
        string address,
        long price,
        bool isNds,
        IEnumerable<string> descriptionList,
        IEnumerable<string> characteristicList,
        IEnumerable<string> photos,
        bool wasProcessed) =>
        (Id, Url, Title, Address, Price, IsNds, DescriptionList, Characteristics, Photos, WasProcessed) =
        (id, url, title, address, price, isNds, [..descriptionList], [..characteristicList], [..photos], wasProcessed);
    
    private PendingToPublishItem(PendingToPublishItem origin) 
        : this(
            origin.Id, 
            origin.Url, 
            origin.Title, 
            origin.Address,
            origin.Price,
            origin.IsNds,
            origin.DescriptionList,
            origin.Characteristics,
            origin.Photos,
            origin.WasProcessed) { }
}