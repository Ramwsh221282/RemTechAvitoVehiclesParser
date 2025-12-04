using System.Text.Json;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed class CataloguePageItem(
    string id,
    Guid catalogueUrlId,
    string payload,
    bool wasProcessed,
    int retryCount) : ISnapshotSource<CataloguePageItem, CataloguePageItemSnapshot>
{
    private readonly string _id = id;
    private readonly Guid _catalogueUrlId = catalogueUrlId;
    private readonly bool _wasProcessed = wasProcessed;
    private readonly int _retryCount = retryCount;
    private readonly string _payload = payload;

    public CataloguePageItemSnapshot GetSnapshot()
    {
        return new(
            Id: _id, 
            CatalogueUrlId: _catalogueUrlId, 
            Payload: _payload,
            WasProcessed: _wasProcessed, 
            RetryCount: _retryCount);
    }

    public CataloguePageItem IncreaseRetry()
    {
        int nextRetryCount = _retryCount + 1;
        return new CataloguePageItem(this, retryCount: nextRetryCount);
    }

    public CataloguePageItem MarkProcessed()
    {
        if (_wasProcessed)
            throw new InvalidOperationException(
                """
                Cannot mark processed.
                Catalogue page item is already processed.
                """
            );
        return new CataloguePageItem(this, wasProcessed: true);
    }
    
    public static CataloguePageItem FromSnapshot(CataloguePageItemSnapshot snapshot)
    {
        return new(
            id: snapshot.Id,
            catalogueUrlId: snapshot.CatalogueUrlId,
            payload: snapshot.Payload,
            wasProcessed: snapshot.WasProcessed,
            retryCount: snapshot.RetryCount);
    }

    public static CataloguePageItem New(string itemId, Guid catalogueUrlId, string url, IReadOnlyList<string> photos)
    {
        return new(
            id: itemId,
            catalogueUrlId: catalogueUrlId,
            payload: new { url, photos },
            wasProcessed: false,
            retryCount: 0);
    }
    
    public CataloguePageItem(
        string id, 
        Guid catalogueUrlId, 
        object payload, 
        bool wasProcessed, 
        int retryCount) :
        this(
            id,
            catalogueUrlId, 
            JsonSerializer.Serialize(payload), 
            wasProcessed, 
            retryCount) { }
    
    private CataloguePageItem(
        CataloguePageItem origin,
        bool? wasProcessed = null,
        int? retryCount = null) :
        this(
            origin._id,
            origin._catalogueUrlId,
            origin._payload,
            wasProcessed ?? origin._wasProcessed,
            retryCount ?? origin._retryCount) { }
}