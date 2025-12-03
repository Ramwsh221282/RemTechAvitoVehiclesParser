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
            wasProcessed.HasValue ? wasProcessed.Value : origin._wasProcessed,
            currentPage.HasValue ? currentPage.Value : origin._currentPage,
            maxPage.HasValue ? maxPage.Value : origin._maxPage
        ) { }
}