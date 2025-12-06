using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed class PaginationEvaluationParserLink(
    Guid id, 
    string url,
    bool wasProcessed,
    int? currentPage,
    int? maxPage) : ISnapshotSource<PaginationEvaluationParserLink, PaginationEvaluationParserLinkSnapshot>
{
    private readonly Guid _id = id;
    private readonly string _url = url;
    private readonly bool _wasProcessed = wasProcessed;
    private readonly int? _currentPage = currentPage;
    private readonly int? _maxPage = maxPage;

    public PaginationEvaluationParserLink MarkProcessed()
    {
        if (_wasProcessed)
            throw new InvalidOperationException(
                """
                Cannot mark link as processed.
                Link is already processed.
                """
                );
        return new PaginationEvaluationParserLink(this, wasProcessed: true);
    }

    public CataloguePageUrl[] BuildCataloguePageUrls()
    {
        if (!_currentPage.HasValue)
            throw new InvalidOperationException(
                """
                Cannot build catalogue page urls from parser link.
                Parser link has no current page initialized.
                """
                );
        
        if (!_maxPage.HasValue)
            throw new InvalidOperationException(
                """
                Cannot build catalogue page urls from parser link.
                Parser link has no max page initialized.
                """
            );
        
        int pageCounter = _currentPage.Value;
        List<CataloguePageUrl> urls = [];
        while (pageCounter <= _maxPage.Value)
        {
            Guid id = Guid.NewGuid();
            string urlValue = $"{_url}?page={pageCounter}";
            urls.Add(new CataloguePageUrl(id: id, linkId: _id, url: urlValue, processed: false, retryCount: 0));
            pageCounter++;
        }
        
        return urls.ToArray();
    }
    
    public PaginationEvaluationParserLink IncrementCurrentPage()
    {
        if (!_currentPage.HasValue)
            throw new InvalidOperationException(
                """
                Cannot increment current page.
                Current page and max page are not initialized.
                """
            );
        int nextCurrentPage = _currentPage.Value + 1;
        return new PaginationEvaluationParserLink(this, currentPage: nextCurrentPage);
    }
    
    public PaginationEvaluationParserLink AddPagination(int currentPage, int maxPage)
    {
        if (_currentPage.HasValue && _maxPage.HasValue)
            throw new InvalidOperationException("Cannot add pagination for parser link as it is already set.");
        return new PaginationEvaluationParserLink(this, currentPage: currentPage, maxPage: maxPage);
    }
    
    public PaginationEvaluationParserLinkSnapshot GetSnapshot()
    {
        return new PaginationEvaluationParserLinkSnapshot(_id, _url, _wasProcessed, _currentPage, _maxPage);
    }

    public static PaginationEvaluationParserLink FromSnapshot(PaginationEvaluationParserLinkSnapshot snapshot)
    {
        return new PaginationEvaluationParserLink(
            id: snapshot.Id,
            url: snapshot.Url,
            wasProcessed: snapshot.WasProcessed,
            currentPage: snapshot.CurrentPage,
            maxPage: snapshot.MaxPage);
    }
    
    private PaginationEvaluationParserLink(
        PaginationEvaluationParserLink origin,
        bool? wasProcessed = null,
        int? currentPage = null,
        int? maxPage = null) 
        : this(
            origin._id,
            origin._url,
            wasProcessed ?? origin._wasProcessed,
            currentPage ?? origin._currentPage,
            maxPage ?? origin._maxPage
        ) { }
}