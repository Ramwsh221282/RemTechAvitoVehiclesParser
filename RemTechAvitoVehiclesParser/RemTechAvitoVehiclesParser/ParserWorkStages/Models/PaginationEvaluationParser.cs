using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed class PaginationEvaluationParser(
    Guid id, 
    string domain, 
    string type,
    IEnumerable<PaginationEvaluationParserLink> links) : ISnapshotSource<PaginationEvaluationParser, PaginationEvaluationParserSnapshot>
{
    private readonly Guid _id = id;
    private readonly string _domain = domain;
    private readonly string _type = type;
    private readonly List<PaginationEvaluationParserLink> _links = [..links];

    public void AddLink(PaginationEvaluationParserLink link)
    {
        _links.Add(link);
    }
    
    public PaginationEvaluationParserSnapshot GetSnapshot()
    {
        return new PaginationEvaluationParserSnapshot(
            _id,
            _domain,
            _type,
            _links.Select(l => l.GetSnapshot()).ToList()
        );
    }
}