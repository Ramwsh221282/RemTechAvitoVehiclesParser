using PuppeteerSharp;
using RemTechAvitoVehiclesParser;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.Utils;

namespace Tests.PuppeteerTests;

public sealed class AvitoCatalogueItemsCollection
{ 
    public IReadOnlyList<AvitoCatalogueItem> Items { get; }

    private AvitoCatalogueItemsCollection(IEnumerable<AvitoCatalogueItem> items)
    {
        Items = [..items];
    }

    public static async Task<AvitoCatalogueItemsCollection> FromCatalogue(IPage page)
    {
        DisposableResourcesManager resourcesManager = new DisposableResourcesManager();
        Maybe<IElementHandle> rootItemsContainer = await page.GetElementRetriable("div.index-root-H81wX");
        resourcesManager = resourcesManager.Add(rootItemsContainer);
        if (!rootItemsContainer.HasValue) return Empty();
        IElementHandle[] catalogueItems = await rootItemsContainer.Value.GetElements("div[data-marker='item']");
        resourcesManager = resourcesManager.Add(catalogueItems);
        if (catalogueItems.Length == 0) return Empty();
        List<AvitoCatalogueItem> items = [];
        foreach (IElementHandle catalogueItem in catalogueItems)
        {
            resourcesManager = resourcesManager.Add(catalogueItem);
            Maybe<AvitoCatalogueItem> item = await AvitoCatalogueItem.FromCatalogueItemElement(catalogueItem, page);
            if (item.HasValue) items.Add(item.Value);
        }

        await resourcesManager.DisposeResources();
        return new AvitoCatalogueItemsCollection(items);
    }

    private static AvitoCatalogueItemsCollection Empty() => new([]);
}