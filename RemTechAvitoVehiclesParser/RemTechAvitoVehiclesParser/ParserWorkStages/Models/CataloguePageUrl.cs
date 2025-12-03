using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed class CataloguePageUrl(Guid id, Guid linkId, string url, bool processed, int retryCount) : ISnapshotSource<CataloguePageUrl, CataloguePageUrlSnapshot>
{
    private readonly Guid _id = id;
    private readonly Guid _linkId = linkId;
    private readonly string _url = url;
    private readonly bool _processed = processed;
    private readonly int _retryCount = retryCount;

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
    
    private CataloguePageUrl(CataloguePageUrl origin, bool? processed = null)
        : this(
            origin._id, 
            origin._linkId, 
            origin._url, 
            processed ?? origin._processed, 
            origin._retryCount) { }

    public CataloguePageUrlSnapshot GetSnapshot()
    {
        return new CataloguePageUrlSnapshot(_id, _linkId, _url, _processed, _retryCount);
    }
}