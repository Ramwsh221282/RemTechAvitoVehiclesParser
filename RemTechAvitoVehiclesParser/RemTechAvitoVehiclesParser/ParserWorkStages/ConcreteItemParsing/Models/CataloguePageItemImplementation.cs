using System.Text.Json;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

public static class CataloguePageItemImplementation
{
    extension(CataloguePageItem item)
    {
        public CataloguePageItem IncreaseRetry()
        {
            int nextRetryCount = item.RetryCount + 1;
            return item with { RetryCount = nextRetryCount };
        }

        public CataloguePageItem MarkProcessed()
        {
            if (item.WasProcessed)
                throw new InvalidOperationException(
                    """
                    Cannot mark processed.
                    Catalogue page item is already processed.
                    """
                );
            return item with { WasProcessed = true };
        }

        public string ReadUrl()
        {
            using JsonDocument document = JsonDocument.Parse(item.Payload);
            return document.RootElement.GetProperty("url").GetString()!;
        }

        public IReadOnlyList<string> ReadPhotos()
        {
            using JsonDocument document = JsonDocument.Parse(item.Payload);
            List<string> photos = new List<string>(document.RootElement.GetProperty("photos").GetArrayLength());
            foreach (JsonElement photo in document.RootElement.GetProperty("photos").EnumerateArray())
                photos.Add(photo.GetString()!);
            return photos;
        }
    }
}