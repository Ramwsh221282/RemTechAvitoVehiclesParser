using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractCatalogueItemData;

public sealed class ExtractCatalogueItemDataCommand(
    Func<Task<IPage>> pageSource,
    CataloguePageUrl pagedUrl,
    AvitoBypassFactory bypassFactory) : IExtractCatalogueItemDataCommand
{
    public async Task<AvitoVehicle[]> Handle()
    {
        const string javaScript = @"
                                  () => {
                                  const photoExtractFn = (item) => {
                                    const photoListSelector = item.querySelector('ul.photo-slider-list-R0jle');
                                    if (!photoListSelector) return [];
                                    return Array.from(photoListSelector.querySelectorAll('li')).map(s => {
                                        const photo = s.querySelector('img');
                                        if (!photo) return '';
                                        const srcSet = photo.getAttribute('srcset');
                                        if (!srcSet) return '';
                                        const splittedParts = srcSet.split(',');
                                        return splittedParts[splittedParts.length-1].split(' ')[0];
                                    });
                                };

                                const itemSelectors = Array.from(document.querySelectorAll('div[data-marker=""item""]'));
                                const data = itemSelectors.map((i) => {
                                      // url extraction    
                                      const urlValue = ""https://avito.ru"" + i.querySelector('h2[itemprop=""name""]')
                                                                                .querySelector('a[itemprop=""url""]')
                                                                                .getAttribute(""href"");
                                      // price and is nds extraction
                                      const priceSelector = i.querySelector('p[data-marker=""item-price""]');
                                      const priceValue = priceSelector.querySelector('meta[itemprop=""price""]').getAttribute(""content"");
                                      const isNds = priceSelector.innerText.includes(""НДС"");
                                      // id
                                      const idValue = ""avito_vehicle_"" + i.getAttribute(""data-item-id"");
                                      // address
                                      const address = i.querySelector('div[data-marker=""item-location""]').querySelector('span[title]').innerText;
                                      // photos
                                      const photos = photoExtractFn(i);                                      
                                      return { url: urlValue, price: priceValue, isNds: isNds, id: idValue, address: address, photos: photos }
                                  });
                                  return data;
                                  }";
        
        IPage page = await pageSource();
        await page.PerformQuickNavigation(pagedUrl.Url);
        if (!await bypassFactory.Create(page).Bypass()) 
            throw new InvalidOperationException("Unable to bypass Avito firewall");
        
        await page.ResilientWaitForSelector("div[id=\"bx_serp-item-list\"]");
        
        return (await page.EvaluateFunctionAsync<JsonConvertedCatalogueItemData[]>(javaScript))
                .Where(d => d.AllPropertiesSet())
                .Select(d => AvitoVehicle.New().Transform(d, 
                    map => AvitoVehicle.RepresentedByCatalogueItem(
                        map,
                        idMap: itemData => itemData.Id!,
                        urlMap: itemData => itemData.Url!,
                        priceMap: itemData => long.Parse(itemData.Price!),
                        isNdsMap: itemData => itemData.IsNds,
                        addressMap: itemData => itemData.Address!,
                        photosMap: itemData => itemData.Photos!
                    )))
                .ToArray();
    }

    private sealed class JsonConvertedCatalogueItemData
    {
        public string? Url { get; set; }
        public string? Price { get; set; }
        public bool IsNds { get; set; }
        public string? Id { get; set; }
        public string? Address { get; set; }
        public string[]? Photos { get; set; }
        public bool AllPropertiesSet() => Url != null && Price != null && Id != null && Address != null && Photos != null;
    }
}