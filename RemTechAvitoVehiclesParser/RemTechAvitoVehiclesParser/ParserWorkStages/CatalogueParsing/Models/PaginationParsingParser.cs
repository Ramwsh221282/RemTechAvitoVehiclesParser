namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public sealed record PaginationParsingParser(
    Guid Id,
    string Domain, 
    string Type, 
    IReadOnlyList<PaginationParsingParserLink> Links);