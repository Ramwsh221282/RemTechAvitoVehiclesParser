namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

public static class CataloguePageUrlConstruction
{
    extension(CataloguePageUrl)
    {
        public static CataloguePageUrl New(Guid linkId, string url) => new
        (
            Id: Guid.NewGuid(),
            LinkId: linkId,
            Url: url,
            Processed: false,
            RetryCount: 0,
            []
        );

        public static CataloguePageUrl MapFrom<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, Guid> linkIdMap,
            Func<T, string> urlMap,
            Func<T, bool> processedMap,
            Func<T, int> retryMap
        ) => new
        (
            Id: idMap(source),
            LinkId: linkIdMap(source),
            Url: urlMap(source),
            Processed: processedMap(source),
            RetryCount: retryMap(source),
            []
        );
    }
}