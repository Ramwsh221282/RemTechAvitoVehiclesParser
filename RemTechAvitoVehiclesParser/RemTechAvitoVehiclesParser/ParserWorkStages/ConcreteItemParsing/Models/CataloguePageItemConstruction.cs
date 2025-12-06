using System.Text.Json;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

public static class CataloguePageItemConstruction
{
    extension(CataloguePageItem)
    {
        public static CataloguePageItem New(string itemId, Guid catalogueUrlId, string url, IReadOnlyList<string> photos) => new
        (
            Id: itemId,
            CatalogueUrlId: catalogueUrlId,
            Payload: JsonSerializer.Serialize(new { url, photos }),
            WasProcessed: false,
            RetryCount: 0
        );

        public static CataloguePageItem MapFrom<T>(
            T source,
            Func<T, string> idMap,
            Func<T, Guid> catalogueIdMap,
            Func<T, string> payloadMap,
            Func<T, bool> processedMap,
            Func<T, int> retryMap
        ) => new
        (
            Id: idMap(source),
            CatalogueUrlId: catalogueIdMap(source),
            Payload: payloadMap(source),
            WasProcessed: processedMap(source),
            RetryCount: retryMap(source)
        );

        public static CataloguePageItem MapFrom<T>(
            T source,
            Func<T, string> idMap,
            Func<T, Guid> catalogueIdMap,
            Func<T, object> payloadMap,
            Func<T, bool> processedMap,
            Func<T, int> retryMap
        ) => MapFrom(source, idMap, catalogueIdMap, s => JsonSerializer.Serialize(payloadMap(s)), processedMap, retryMap);
    }
}