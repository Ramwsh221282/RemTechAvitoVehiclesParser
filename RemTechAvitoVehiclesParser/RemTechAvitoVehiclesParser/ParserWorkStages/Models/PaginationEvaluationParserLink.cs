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

    public PaginationEvaluationParserLink MarkAsProcessed(int currentPage, int maxPage)
    {
        if (_wasProcessed)
            throw new InvalidOperationException("Cannot mark processing parser as it is already processed");
        return new PaginationEvaluationParserLink(this, wasProcessed: true, currentPage: currentPage, maxPage: maxPage);
    }
    
    public PaginationEvaluationParserLinkSnapshot GetSnapshot()
    {
        return new PaginationEvaluationParserLinkSnapshot(_id, _url, _wasProcessed, _currentPage, _maxPage);
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