using System.Text;
using ParsingSDK;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace Tests.PuppeteerTests;

public sealed class AvitoPagination
{
    public IPage Page { get; }
    public string Url { get; }
    public int CurrentPageValue { get; private set; }
    public int MaxPageValue { get; }

    private AvitoPagination(int currentPage, int maxPage, string url, IPage page)
    {
        CurrentPageValue = currentPage;
        MaxPageValue = maxPage;
        Url = url;
        Page = page;
    }

    public async Task ProcessWhileNotReachedMaxPage(Func<Task> fn)
    {
        while (MaxPageValue >= CurrentPageValue)
        {
            string url = BuildNextPageUrl();
            await Page.NavigatePage(url);
            await fn();
            CurrentPageValue++;
        }
    }

    private string BuildNextPageUrl()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(Url);
        sb.Append($"?p={CurrentPageValue}");
        return sb.ToString();
    }
    
    public static async Task<AvitoPagination> FromCatalogue(IPage page, string url)
    {
        Maybe<IElementHandle> element = await page.GetElementRetriable("nav[aria-label='Пагинация']");
        if (!element.HasValue) return SinglePaged(page, url);
        IElementHandle[] paginationElements = await element.Value.GetElements("li");
        if (paginationElements.Length == 0) return SinglePaged(page, url);
        SelectedAvitoPage current = await SelectedAvitoPage.FromPaginationElements(paginationElements);
        MaxAvitoPage maxPage = await MaxAvitoPage.FromPaginationElements(paginationElements);
        return new AvitoPagination(current.Value, maxPage.Value, url, page);
    }

    private static AvitoPagination SinglePaged(IPage page, string url)
    {
        return new AvitoPagination(0, 1, url, page);
    }
}