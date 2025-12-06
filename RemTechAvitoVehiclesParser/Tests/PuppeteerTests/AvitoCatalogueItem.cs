using ParsingSDK;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace Tests.PuppeteerTests;

public sealed class AvitoCatalogueItem
{
    public IReadOnlyList<string> ImageUrls { get; }
    public string ItemId { get; }
    public string ItemUrl { get; }

    private AvitoCatalogueItem(string itemId, string itemUrl, IEnumerable<string> imageUrls)
    {
        ImageUrls = [..imageUrls];
        ItemId = itemId;
        ItemUrl = itemUrl;
    }

    public static async Task<Maybe<AvitoCatalogueItem>> FromCatalogueItemElement(IElementHandle catalogueItem, IPage page)
    {
        DisposableResourcesManager resourcesManager = new DisposableResourcesManager();
        
        Maybe<string> itemId = await catalogueItem.GetAttribute("data-item-id");
        if (!itemId.HasValue) Maybe<AvitoCatalogueItem>.None();

        Maybe<IElementHandle> titleContainer = await catalogueItem.GetElementRetriable("div.iva-item-listTopBlock-n6Rva");
        resourcesManager = resourcesManager.Add(titleContainer);
        if (!titleContainer.HasValue) return Maybe<AvitoCatalogueItem>.None();

        Maybe<IElementHandle> itemUrlContainer = await titleContainer.Value.GetElementRetriable("a[itemprop='url']");
        resourcesManager = resourcesManager.Add(itemUrlContainer);
        if (!itemUrlContainer.HasValue) return Maybe<AvitoCatalogueItem>.None();

        Maybe<string> itemUrlAttribueValue = await itemUrlContainer.Value.GetAttribute("href");
        if (!itemUrlAttribueValue.HasValue) return Maybe<AvitoCatalogueItem>.None();
        
        Maybe<IElementHandle> itemImage = await catalogueItem.GetElementRetriable("div[data-marker='item-image']");
        resourcesManager = resourcesManager.Add(itemImage);
        if (!itemImage.HasValue) Maybe<AvitoCatalogueItem>.None();
        await itemImage.Value.HoverAsync();

        Maybe<IElementHandle> updatedItemImage = await page.GetElementRetriable($"div[data-marker='item'][data-item-id='{itemId.Value}']");
        resourcesManager = resourcesManager.Add(updatedItemImage);
        if (!updatedItemImage.HasValue) Maybe<AvitoCatalogueItem>.None();
            
        Maybe<IElementHandle> photoSliderList = await updatedItemImage.Value.GetElementRetriable("ul.photo-slider-list-R0jle");
        resourcesManager = resourcesManager.Add(photoSliderList);
        if (!photoSliderList.HasValue) Maybe<AvitoCatalogueItem>.None();

        IElementHandle[] photoElements = await photoSliderList.Value.GetElements("li");
        resourcesManager = resourcesManager.Add(photoElements);
        
        List<string> photos = [];
        foreach (IElementHandle photo in photoElements)
        {
            Maybe<IElementHandle> imageElement = await photo.GetElementRetriable("img");
            resourcesManager.Add(imageElement);
            if (!imageElement.HasValue) continue;
            Maybe<string> srcSetAttribute = await imageElement.Value.GetAttribute("srcset");
            if (!srcSetAttribute.HasValue) continue;
            string[] sets = srcSetAttribute.Value.Split(',');
            string highQualityImageUrl = sets[^1].Split(' ')[0];
            photos.Add(highQualityImageUrl);
        }

        string itemIdValue = itemId.Value;
        string itemUrlValue = $"https://avito.ru{itemUrlAttribueValue.Value}";
        await resourcesManager.DisposeResources();
        return Maybe<AvitoCatalogueItem>.Some(new AvitoCatalogueItem(itemIdValue, itemUrlValue, photos));
    }
}