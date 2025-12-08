using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace Tests.PuppeteerTests;

public sealed class MaxAvitoPage
{
    public int Value { get; }

    private MaxAvitoPage(int value)
    {
        Value = value;
    }

    public static async Task<MaxAvitoPage> FromPaginationElements(IElementHandle[] elements)
    {
        int maxPage = 0;
        foreach (IElementHandle pageElements in elements)
        {
            Maybe<IElementHandle> pageNumberElement = await pageElements.GetElementRetriable("span.styles-module-text-Z0vDE");
            if (!pageNumberElement.HasValue) continue;
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue) continue;
            if (!int.TryParse(pageNumberText.Value, out int maxPageValue)) continue;
            if (maxPage < maxPageValue)
                maxPage = maxPageValue;
        }
        
        return new MaxAvitoPage(maxPage);
    }
}