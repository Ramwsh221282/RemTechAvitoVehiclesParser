namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public static class PaginationParsingParserImplementation
{
    extension(PaginationParsingParser parser)
    {
        public PaginationParsingParser AddLink(PaginationParsingParserLink link) =>
            parser with { Links = [link, ..parser.Links] };

        public PaginationParsingParser AddLinks(IEnumerable<PaginationParsingParserLink> links)
        {
            return parser with { Links = [..links] };
        }

        public bool AllLinksHavePagesInitialized()
        {
            return parser.Links.All(l => l.CurrentPage.HasValue && l.MaxPage.HasValue);
        }
    }
}