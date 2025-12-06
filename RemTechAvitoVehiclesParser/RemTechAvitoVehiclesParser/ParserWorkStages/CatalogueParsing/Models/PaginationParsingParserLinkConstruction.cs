namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public static class PaginationParsingParserLinkConstruction
{
    extension(PaginationParsingParserLink)
    {
        public static PaginationParsingParserLink MapFrom<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, Guid> parserIdMap,
            Func<T, string> urlMap,
            Func<T, bool> processedMap,
            Func<T, int?> currentPageMap,
            Func<T, int?> maxPageMap
        ) => new
        (
            Id: idMap(source),
            ParserId: parserIdMap(source),
            Url: urlMap(source),
            WasProcessed: processedMap(source),
            CurrentPage: currentPageMap(source),
            MaxPage: maxPageMap(source)
        );
        
        public static PaginationParsingParserLink NewFromParser(PaginationParsingParser parser, string url) => new
        (
            Id: Guid.NewGuid(), 
            ParserId: parser.Id, 
            Url: url, 
            WasProcessed: false,
            CurrentPage: null,
            MaxPage: null
        );

        public static PaginationParsingParserLink FromParser(PaginationParsingParser parser, Guid id, string url) =>
            new(
                Id: id,
                ParserId: parser.Id,
                Url: url,
                WasProcessed: false,
                CurrentPage: null,
                MaxPage: null
            );
    }
}