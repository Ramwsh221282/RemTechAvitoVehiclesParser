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
        return new CataloguePageItemSnapshot(
            Id: _id, 
            CatalogueUrlId: _catalogueUrlId, 
            Payload: _payload,
            WasProcessed: _wasProcessed, 
            RetryCount: _retryCount);
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