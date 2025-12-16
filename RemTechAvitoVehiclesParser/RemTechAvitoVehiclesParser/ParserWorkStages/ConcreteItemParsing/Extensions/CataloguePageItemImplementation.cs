using System.Text.Json;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages;
using RemTechAvitoVehiclesParser.Parsing;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;

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
            List<string> photos = new(document.RootElement.GetProperty("photos").GetArrayLength());
            foreach (JsonElement photo in document.RootElement.GetProperty("photos").EnumerateArray())
                photos.Add(photo.GetString()!);
            return photos;
        }

        private async Task<PendingToPublishItem> CreatePendingItem(
          IBrowser browser,
          WorkStageProcessDependencies deps
        )
        {
            string url = item.ReadUrl();
            IReadOnlyList<string> photos = item.ReadPhotos();
            AvitoSpecialEquipmentAdvertisement advertisement =
              await AvitoSpecialEquipmentAdvertisement.Create(await browser.GetPage(), url, deps.Bypasses);
            if (!await advertisement.IsValid())
                throw new InvalidOperationException("Invalid advertisement.");

            AvitoSpecialEquipmentAdvertisementSnapshot snapshot = advertisement.GetSnapshot();
            return CreatePendingItem(item.Id, url, photos, snapshot);
        }

        private static PendingToPublishItem CreatePendingItem(
          string id,
          string url,
          IEnumerable<string> photos,
          AvitoSpecialEquipmentAdvertisementSnapshot advertisement
        )
        {
            return new(
              Id: id,
              Url: url,
              Title: advertisement.Title,
              Address: advertisement.Address,
              Price: advertisement.Price,
              IsNds: advertisement.IsNds,
              DescriptionList: advertisement.DescriptionList,
              Characteristics: advertisement.Characteristics,
              Photos: [.. photos],
              WasProcessed: false
            );
        }
    }
}

}