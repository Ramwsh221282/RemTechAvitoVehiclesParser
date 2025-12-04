using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed class CataloguePageUrl(
    Guid id, 
    Guid linkId, 
    string url, 
    bool processed, 
    int retryCount) :
    ISnapshotSource<CataloguePageUrl, CataloguePageUrlSnapshot>
{
    private readonly Guid _id = id;
    private readonly Guid _linkId = linkId;
    private readonly string _url = url;
    private readonly bool _processed = processed;
    private readonly int _retryCount = retryCount;
    private readonly List<CataloguePageItem> _items = [];

    public CataloguePageUrl MarkProcessed()
    {
        if (_processed)
            throw new InvalidOperationException(
                """
                Cannot mark catalogue page url as processed.
                Catalogue page url is already processed.
                """);
        return new CataloguePageUrl(this, processed: true);
    }

    public IEnumerable<CataloguePageItem> Items()
    {
        return _items;
    }
    
    public CataloguePageUrl IncremenetRetryCount()
    {
        int nextRetryCount = _retryCount + 1;
        return new CataloguePageUrl(this, retryCount: nextRetryCount);
    }
    
    private CataloguePageUrl(
        CataloguePageUrl origin, 
        bool? processed = null,
        int? retryCount = null)
        : this(
            origin._id, 
            origin._linkId, 
            origin._url, 
            processed ?? origin._processed, 
            retryCount ?? origin._retryCount) { }

    public void AddItems(IEnumerable<CataloguePageItem> items)
    {
        _items.AddRange(items);
    }
    
    public CataloguePageUrlSnapshot GetSnapshot()
    {
        return new CataloguePageUrlSnapshot(
            _id, 
            _linkId, 
            _url, 
            _processed, 
            _retryCount);
    }

    public static CataloguePageUrl FromSnapshot(CataloguePageUrlSnapshot snapshot)
    {
        return new CataloguePageUrl(
            snapshot.Id,
            snapshot.LinkId,
            snapshot.Url,
            snapshot.Processed,
            snapshot.RetryCount);
    }
}