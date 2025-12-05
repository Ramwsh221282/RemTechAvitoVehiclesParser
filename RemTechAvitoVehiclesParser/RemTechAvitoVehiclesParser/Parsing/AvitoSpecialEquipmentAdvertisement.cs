using PuppeteerSharp;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;
using RemTechAvitoVehiclesParser.Utilities.TextTransforming;

namespace RemTechAvitoVehiclesParser.Parsing;

public sealed class AvitoSpecialEquipmentAdvertisementSnapshot : ISnapshot
{
    public required string Title { get; init; }
    public required long Price { get; init; }
    public required bool IsNds { get; init; }
    public required string Address { get; init; }
    public required IReadOnlyList<string> Characteristics { get; init; }
    public required IReadOnlyList<string> DescriptionList { get; init; }
}

public sealed class AvitoSpecialEquipmentAdvertisement : ISnapshotSource<AvitoSpecialEquipmentAdvertisement, AvitoSpecialEquipmentAdvertisementSnapshot>
{
    private readonly Dictionary<string, object> _properties = [];
    private readonly IPage _page;
    private readonly bool _bypassedBlock;

    public async Task<bool> HasTitle()
    {
        const string propertyName = "title";
        if (_properties.ContainsKey(propertyName)) return true;
        if (!_bypassedBlock) return false;
        Maybe<IElementHandle> breadcrumbsContainer = await _page.GetElementRetriable("div[id='bx_item-breadcrumbs']");
        if (!breadcrumbsContainer.HasValue) return false;
        IElementHandle[] elements = await breadcrumbsContainer.Value.GetElements("span[itemprop='itemListElement']");
        Maybe<string> model = await elements[^1].GetElementInnerText();
        Maybe<string> brand = await elements[^2].GetElementInnerText();
        Maybe<string> type = await elements[^3].GetElementInnerText();
        if (!model.HasValue || !brand.HasValue || !type.HasValue) return false;
        string title = $"{type.Value} {brand.Value} {model.Value}";
        _properties.Add(propertyName, title);
        return true;
    }

    public async Task<bool> HasDescription(ITextTransformer transformer)
    {
        const string propertyName = "description_list";
        if (_properties.ContainsKey(propertyName)) return true;
        if (!_bypassedBlock) return false;
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
        if (!_bypassedBlock) return false;
        List<string> characteristics = [];
        Maybe<IElementHandle> characteristicsContainer = await _page.GetElementRetriable("ul.params__paramsList___XzY3MG");
        if (!characteristicsContainer.HasValue) return false;
        IElementHandle[] characteristicNodes = await characteristicsContainer.Value.GetElements("li");
        if (characteristicNodes.Length == 0) return false;
        foreach (IElementHandle characteristicNode in characteristicNodes)
        {
            Maybe<string> nameValuePair = await characteristicNode.GetElementInnerText();
            if (!nameValuePair.HasValue) continue;
            string[] splitted = nameValuePair.Value.Trim().Split(':');
            string characteristic = $"{splitted[0]} {splitted[^1]}";
            characteristics.Add(characteristic);
        }
        if (characteristics.Count == 0) return false;
        _properties.Add(characteristicsPropertyName, characteristics);
        return true;
    }

    public async Task<bool> HasAddress(ITextTransformer transformer)
    {
        const string addressPropertyName = "address";
        if (_properties.ContainsKey(addressPropertyName)) return true;
        if (!_bypassedBlock) return false;
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

    public AvitoSpecialEquipmentAdvertisementSnapshot GetSnapshot() => new()
    {
        Address = GetAddress(),
        Characteristics = GetCharacteristics(),
        IsNds = GetIsNds(),
        Price = GetPrice(),
        DescriptionList = GetDescription(),
        Title = GetTitle()
    };

    private string GetAddress() => (_properties["address"] as string)!;
    private IReadOnlyList<string> GetCharacteristics() => (_properties["characteristics"] as IReadOnlyList<string>)!;
    private long GetPrice() => (long)_properties["price"];
    private bool GetIsNds() => (bool)_properties["is_nds"];
    private string GetTitle() => (_properties["title"] as string)!;
    private IReadOnlyList<string> GetDescription() => (_properties["description_list"] as IReadOnlyList<string>)!;
}