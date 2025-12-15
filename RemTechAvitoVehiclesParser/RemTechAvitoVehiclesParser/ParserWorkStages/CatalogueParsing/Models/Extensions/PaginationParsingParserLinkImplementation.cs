using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models.Extensions;

public static class PaginationParsingParserLinkImplementation
{
    extension(ProcessingParserLink origin)
    {
        public ProcessingParserLink MarkProcessed()
        {
            if (origin.WasProcessed)
                throw new InvalidOperationException(
                    """
                    Cannot mark link as processed.
                    Link is already processed.
                    """
                );
            return origin with { WasProcessed = true };
        }

        public ProcessingParserLink IncreaseRetryCount()
        {
            int next = origin.RetryCount + 1;
            return origin with { RetryCount = next };
        }

        public async Task<CataloguePageUrl[]> ExtractPaginatedUrls(
            IBrowser browser,
            AvitoBypassFactory bypassFactory
        )
        {
            Maybe<IPage> page = await PreparedPage(browser, bypassFactory, origin.Url);
            if (!page.HasValue)
                throw new InvalidOperationException(
                    "Unable to get proper page state for pagination extraction"
                );

            IElementHandle[] paginationElements = await GetPaginationElements(page.Value);
            int currentPage = await GetCurrentPageFromPaginationContainer(paginationElements);
            int maxPage = await GetMaxPageFromPaginationContainer(paginationElements);

            CataloguePageUrl[] urls = new CataloguePageUrl[maxPage + 1];
            for (int i = currentPage; i <= maxPage; i++)
            {
                string urlValue = $"{origin.Url}&page={i}";
                urls[i] = CataloguePageUrl.New(urlValue);
            }

            return urls;
        }
    }

    private static async Task<Maybe<IPage>> PreparedPage(
        IBrowser browser,
        AvitoBypassFactory bypasses,
        string url
    )
    {
        IPage page = await browser.GetPage();
        await page.NavigatePage(url);
        if (!await bypasses.Create(page).Bypass())
            return Maybe<IPage>.None();
        await page.ScrollBottom();
        await page.ScrollTop();
        return Maybe<IPage>.Some(page);
    }

    private static async Task<IElementHandle[]> GetPaginationElements(IPage page)
    {
        Maybe<IElementHandle> element = await page.GetElementRetriable(
            "nav[aria-label='Пагинация']"
        );
        if (!element.HasValue)
            return [];
        IElementHandle[] paginationElements = await element.Value.GetElements("li");
        if (paginationElements.Length == 0)
            return [];
        return paginationElements;
    }

    private static async Task<int> GetCurrentPageFromPaginationContainer(IElementHandle[] elements)
    {
        int currentPage = 0;
        foreach (IElementHandle element in elements)
        {
            Maybe<IElementHandle> selectedPage = await element.GetElementRetriable(
                "span[aria-current='page']"
            );
            if (!selectedPage.HasValue)
                continue;
            Maybe<IElementHandle> pageNumberElement = await selectedPage.Value.GetElementRetriable(
                "span.styles-module-text-Z0vDE"
            );
            if (!pageNumberElement.HasValue)
                continue;
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue)
                continue;
            currentPage = int.Parse(pageNumberText.Value);
            break;
        }

        return currentPage;
    }

    private static async Task<int> GetMaxPageFromPaginationContainer(IElementHandle[] elements)
    {
        int maxPage = 0;
        foreach (IElementHandle element in elements)
        {
            Maybe<IElementHandle> pageNumberElement = await element.GetElementRetriable(
                "span.styles-module-text-Z0vDE"
            );
            if (!pageNumberElement.HasValue)
                continue;
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue)
                continue;
            if (!int.TryParse(pageNumberText.Value, out int maxPageValue))
                continue;
            if (maxPage < maxPageValue)
                maxPage = maxPageValue;
        }

        return maxPage;
    }
}
