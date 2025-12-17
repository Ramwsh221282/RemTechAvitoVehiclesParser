using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.HoverCatalogueItemImages;

public sealed class HoverCatalogueItemImagesCommand(Func<Task<IPage>> pageSource) : IHoverCatalogueItemImagesCommand
{
    public async Task Handle()
    {
        IPage page = await pageSource();
        IElementHandle[] catalogueItems = await page.GetElementsRetriable("div[data-marker='item']");
        if (catalogueItems.Length == 0) return;

        foreach (IElementHandle catalogueItem in catalogueItems)
        {
            Maybe<IElementHandle> itemImage = await catalogueItem.GetElementRetriable("div[data-marker='item-image']");
            if (!itemImage.HasValue) continue;
            await itemImage.Value.HoverAsync();
        }
    }
}