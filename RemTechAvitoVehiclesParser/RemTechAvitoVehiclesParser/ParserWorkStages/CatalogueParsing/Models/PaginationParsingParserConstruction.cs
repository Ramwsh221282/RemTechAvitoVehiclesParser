namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public static class PaginationParsingParserConstruction
{
    extension(PaginationParsingParser)
    {
        public static PaginationParsingParser WithoutLinks(Guid id, string domain, string type)
        {
            return new PaginationParsingParser(id, domain, type, []);
        }
    }
}