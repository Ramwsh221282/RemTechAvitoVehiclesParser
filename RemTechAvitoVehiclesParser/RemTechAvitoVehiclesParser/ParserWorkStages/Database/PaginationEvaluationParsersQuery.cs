namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed record PaginationEvaluationParsersQuery(
    Guid? ParserId = null, 
    bool WithLock = false,
    bool LinksWithoutCurrentPage = false,
    bool LinksWithoutMaxPage = false,
    bool LinksWithCurrentPage = false,
    bool LinksWithMaxPage = false,
    bool OnlyNotProcessedLinks = false,
    bool OnlyProcessedLinks = false,
    int? LinksLimit = 50);