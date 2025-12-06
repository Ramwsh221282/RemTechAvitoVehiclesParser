using PuppeteerSharp;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace Tests.PuppeteerTests;

public sealed class SelectedAvitoPage
{
    public int Value { get; }

    private SelectedAvitoPage(int value)
    {
        Value = value;
    }

    public static async Task<SelectedAvitoPage> FromPaginationElements(IElementHandle[] elements)
    {
        int currentPage = 0;
        foreach (IElementHandle paginationElement in elements)
        {
            Maybe<IElementHandle> selectedPage = await paginationElement
                .GetElementRetriable("span[aria-current='page']");
            if (!selectedPage.HasValue) continue;
            
            Maybe<IElementHandle> pageNumberElement = await selectedPage
                .Value.GetElementRetriable("span.styles-module-text-Z0vDE");
            if (!pageNumberElement.HasValue) continue;
            
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue) continue;
            currentPage = int.Parse(pageNumberText.Value);
            break;
        }

        return new SelectedAvitoPage(currentPage);
    }
}