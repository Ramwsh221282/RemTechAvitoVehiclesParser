namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public record PaginationParsingParserLink(
    Guid Id,
    Guid ParserId,
    string Url,
    bool WasProcessed,
    int? CurrentPage,
    int? MaxPage);