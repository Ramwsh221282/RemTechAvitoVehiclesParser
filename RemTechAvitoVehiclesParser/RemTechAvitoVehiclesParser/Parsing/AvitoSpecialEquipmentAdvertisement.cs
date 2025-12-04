using PuppeteerSharp;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.Utilities.TextTransforming;

namespace RemTechAvitoVehiclesParser.Parsing;

public sealed class AvitoSpecialEquipmentAdvertisement
{
    private readonly Dictionary<string, object> _properties = [];
    private readonly IPage _page;
    private readonly bool _bypassedBlock;

    public async Task<bool> HasTitle()
    {
        const string propertyName = "title";
        if (_properties.ContainsKey(propertyName)) return true;
        if (!_bypassedBlock) return false;
        Maybe<IElementHandle> titleContainer = await _page.GetElementRetriable("div.js-item-view-title-info");
        if (!titleContainer.HasValue) return false;
        Maybe<IElementHandle> titleElement = await titleContainer.Value.GetElementRetriable("h1[itemprop='name']");
        if (!titleElement.HasValue) return false;
        Maybe<string> titleValue = await titleElement.Value.GetElementInnerText();
        if (!titleValue.HasValue) return false;
        _properties.Add(propertyName, titleValue.Value);
        return true;
    }

    public async Task<bool> HasDescription(ITextTransformer transformer)
    {
        const string propertyName = "description_list";
        if (_properties.ContainsKey(propertyName)) return true;
        List<string> descriptions = [];
        Maybe<IElementHandle> descriptionContainer = await _page.GetElementRetriable("div[id='bx_item-description']");
        if (!descriptionContainer.HasValue) return false;
        Maybe<IElementHandle> elementWithDescription = await descriptionContainer.Value.GetElementRetriable("div[data-marker='item-view/item-description']");
        if (!elementWithDescription.HasValue) return false;
        IElementHandle[] descriptionParts = await elementWithDescription.Value.GetElements("p");
        if (descriptionParts.Length == 0) return false;
        foreach (IElementHandle descriptionPart in descriptionParts)
        {
            Maybe<string> text = await descriptionPart.GetElementInnerText();
            if (!text.HasValue) continue;
            string transformed = transformer.TransformText(text.Value);
            descriptions.Add(transformed);
        }
        if (descriptions.Count == 0) return false;
        _properties.Add(propertyName, descriptions);
        return true;
    }
    
    public async Task<bool> HasPrice()
    {
        const string pricePropertyName = "price";
        const string isNdsPropertyName = "is_nds";
        if (_properties.ContainsKey(pricePropertyName)) return true;
        if (!_bypassedBlock) return false;
        Maybe<IElementHandle> priceContainer = await _page.GetElementRetriable("div.styles__item-price___ZDZjZj");
        if (!priceContainer.HasValue) return false;
        Maybe<IElementHandle> elementWithPriceValueContainer = await _page.GetElementRetriable("span[id='bx_item-price-value']");
        if (!elementWithPriceValueContainer.HasValue) return false;
        Maybe<IElementHandle> elementWithPriceAttribute = await elementWithPriceValueContainer.Value.GetElementRetriable("span[itemprop='price']");
        Maybe<string> priceValue = await elementWithPriceAttribute.Value.GetAttribute("content");
        if (!priceValue.HasValue) return false;
        bool isPriceParsableToLongNumber = long.TryParse(priceValue.Value, out long priceResult);
        if (!isPriceParsableToLongNumber) return false;
        _properties.Add(pricePropertyName, priceResult);
        Maybe<string> fullText = await priceContainer.Value.GetElementInnerText();
        if (fullText.HasValue && fullText.Value.Contains("НДС", StringComparison.OrdinalIgnoreCase)) _properties.Add(isNdsPropertyName, true);
        else _properties.Add(isNdsPropertyName, false);
        return true;
    }

    public async Task<bool> HasCharacteristics()
    {
        const string characteristicsPropertyName = "characteristics";
        if (_properties.ContainsKey(characteristicsPropertyName)) return true;
        List<string> characteristics = [];
        Maybe<IElementHandle> characteristicsContainer = await _page.GetElementRetriable("ul.params__paramsList___XzY3MG");
        if (!characteristicsContainer.HasValue) return false;
        IElementHandle[] characteristicNodes = await characteristicsContainer.Value.GetElements("li");
        if (characteristicNodes.Length == 0) return false;
        foreach (IElementHandle characteristicNode in characteristicNodes)
        {
            Maybe<IElementHandle> valueContainer = await characteristicNode.GetElementRetriable("span");
            if (!valueContainer.HasValue) continue;
            Maybe<string> value = await valueContainer.Value.GetElementInnerText();
            if (!value.HasValue) return false;
            characteristics.Add(value.Value);
        }
        if (characteristics.Count == 0) return false;
        _properties.Add(characteristicsPropertyName, characteristics);
        return true;
    }

    public async Task<bool> HasAddress(ITextTransformer transformer)
    {
        const string addressPropertyName = "address";
        if (_properties.ContainsKey(addressPropertyName)) return true;
        Maybe<IElementHandle> addressContainer = await _page.GetElementRetriable("div.style__item-map___XzQ5MT");
        if (!addressContainer.HasValue) return false;
        Maybe<IElementHandle> locationContainer = await addressContainer.Value.GetElementRetriable("div.style__item-map-location___XzQ5MT");
        if (!locationContainer.HasValue) return false;
        Maybe<IElementHandle> locationValueContainer = await locationContainer.Value.GetElementRetriable("div.style__item-address___XzQ5MT");
        if (!locationValueContainer.HasValue) return false;
        Maybe<string> locationValue = await locationValueContainer.Value.GetElementInnerText();
        if (!locationValue.HasValue) return false;
        string transformed = transformer.TransformText(locationValue.Value);
        _properties.Add(addressPropertyName, transformed);
        return true;
    }
    
    public static async Task<AvitoSpecialEquipmentAdvertisement> Create(
        IPage page, 
        string itemUrl, 
        AvitoBypassFactory bypassFactory)
    {
        await page.NavigatePage(itemUrl);
        bool bypassed = await bypassFactory.Create(page).Bypass();
        await page.ScrollBottom();
        return new AvitoSpecialEquipmentAdvertisement(page, bypassed);
    }
    
    private AvitoSpecialEquipmentAdvertisement(IPage page, bool bypassedBlock)
    {
        _page = page;
        _bypassedBlock = bypassedBlock;
    }
}