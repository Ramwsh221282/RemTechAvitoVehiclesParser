using System.Text.Json;
using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;

public static class CataloguePageUrlImplementation
{
    extension(CataloguePageUrl url)
    {
        public CataloguePageUrl MarkProcessed()
        {
            if (url.Processed)
                throw new InvalidOperationException(
                    """
                    Cannot mark catalogue page url as processed.
                    Catalogue page url is already processed.
                    """
                );
            return url with { Processed = true };
        }

        public CataloguePageUrl IncrementRetryCount()
        {
            int nextRetryCount = url.RetryCount + 1;
            return url with { RetryCount = nextRetryCount };
        }

        public async Task<CataloguePageItem[]> ExtractPageItems(
            IBrowser browser,
            AvitoBypassFactory bypassFactory)
        {
            await (await browser.GetPage()).NavigatePage(url.Url);
            if (!await bypassFactory.Create(await browser.GetPage()).Bypass())
                return [];

            await (await browser.GetPage()).ScrollBottom();
            await (await browser.GetPage()).ScrollTop();

            IElementHandle[] catalogueElements = await GetCatalogueElements(
                await browser.GetPage()
            );
            List<CataloguePageItem> items = [];
            await foreach (
                (string Id, string Url, IReadOnlyList<string> Photos) in GetCatalogueItemsMetadata(
                    catalogueElements,
                    await browser.GetPage()
                )
            )
                items.Add(CataloguePageItem.New(Id, Url, JsonSerializer.Serialize(Photos)));

            return [.. items];
        }
    }

    private static async Task<IElementHandle[]> GetCatalogueElements(IPage page)
    {
        Maybe<IElementHandle> rootItemsContainer = await page.GetElementRetriable(
            "div.index-root-H81wX"
        );
        if (!rootItemsContainer.HasValue)
            return [];
        IElementHandle[] catalogueItems = await rootItemsContainer.Value.GetElements(
            "div[data-marker='item']"
        );
        return catalogueItems;
    }

    private static async IAsyncEnumerable<(
        string Id,
        string Url,
        IReadOnlyList<string> Photos
    )> GetCatalogueItemsMetadata(IElementHandle[] catalogueItems, IPage page)
    {
        foreach (IElementHandle catalogueItem in catalogueItems)
        {
            Maybe<string> itemId = await catalogueItem.GetAttribute("data-item-id");
            if (!itemId.HasValue)
                continue;

            Maybe<IElementHandle> titleContainer = await catalogueItem.GetElementRetriable(
                "div.iva-item-listTopBlock-n6Rva"
            );
            if (!titleContainer.HasValue)
                continue;

            Maybe<IElementHandle> itemUrlContainer = await titleContainer.Value.GetElementRetriable(
                "a[itemprop='url']"
            );
            if (!itemUrlContainer.HasValue)
                continue;

            Maybe<string> itemUrlAttribueValue = await itemUrlContainer.Value.GetAttribute("href");
            if (!itemUrlAttribueValue.HasValue)
                continue;

            Maybe<IElementHandle> itemImage = await catalogueItem.GetElementRetriable(
                "div[data-marker='item-image']"
            );
            if (!itemImage.HasValue)
                continue;
            await itemImage.Value.HoverAsync();

            Maybe<IElementHandle> updatedItemImage = await page.GetElementRetriable(
                $"div[data-marker='item'][data-item-id='{itemId.Value}']"
            );
            if (!updatedItemImage.HasValue)
                continue;

            Maybe<IElementHandle> photoSliderList =
                await updatedItemImage.Value.GetElementRetriable("ul.photo-slider-list-R0jle");
            if (!photoSliderList.HasValue)
                continue;

            IElementHandle[] photoElements = await photoSliderList.Value.GetElements("li");
            IReadOnlyList<string> photos = await GetItemPhotos(photoElements);

            string itemIdValue = itemId.Value;
            string itemUrlValue = $"https://avito.ru{itemUrlAttribueValue.Value}";
            yield return (itemIdValue, itemUrlValue, photos);
        }
    }

    private static async Task<IReadOnlyList<string>> GetItemPhotos(IElementHandle[] photoElements)
    {
        List<string> photos = [];
        foreach (IElementHandle photo in photoElements)
        {
            Maybe<IElementHandle> imageElement = await photo.GetElementRetriable("img");
            if (!imageElement.HasValue)
                continue;
            Maybe<string> srcSetAttribute = await imageElement.Value.GetAttribute("srcset");
            if (!srcSetAttribute.HasValue)
                continue;
            string[] sets = srcSetAttribute.Value.Split(',');
            string highQualityImageUrl = sets[^1].Split(' ')[0];
            photos.Add(highQualityImageUrl);
        }

        return photos;
    }
}
