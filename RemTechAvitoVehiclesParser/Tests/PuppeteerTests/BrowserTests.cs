using PuppeteerSharp;
using RemTechAvitoVehiclesParser;
using RemTechAvitoVehiclesParser.FirewallBypass;

namespace Tests.PuppeteerTests;

public sealed class BrowserTests(PuppeteerTestFixture fixture) : IClassFixture<PuppeteerTestFixture>
{
    [Fact]
    private async Task Hover_Avito_Advertisement_Photos()
    {
        const string targetUrl =
            "https://www.avito.ru/all/gruzoviki_i_spetstehnika/pogruzchiki-ASgBAgICAURU4E0";

        BrowserFactory factory = new BrowserFactory();
        IBrowser browser = await factory.ProvideBrowser(headless: false);
        IPage page = await browser.GetPage();
        await page.NavigatePage(targetUrl);
        bool solved = await new AvitoByPassFirewallWithRetry(new AvitoBypassFirewallLazy(page, new AvitoBypassFirewall(page))).Bypass();
        if (!solved) return;
        await page.ScrollBottom();
        await page.ScrollTop();
        AvitoPagination pagination = await AvitoPagination.FromCatalogue(page, targetUrl);
        await pagination.ProcessWhileNotReachedMaxPage(async () =>
        {
            AvitoCatalogueItemsCollection items = await AvitoCatalogueItemsCollection.FromCatalogue(page);
            foreach (var catalogueItem in items.Items)
            {
                string url = catalogueItem.ItemUrl;
                await page.NavigatePage(url);
                Maybe<IElementHandle> titleElement = await page.GetElementRetriable("div.js-item-view-title-info", retryAmount: 3);
                if (titleElement.HasValue == false)
                {
                    solved = await new AvitoByPassFirewallWithRetry(new AvitoBypassFirewallLazy(page, new AvitoBypassFirewall(page))).Bypass();
                    if (!solved)
                    {
                        continue;
                    }
                }
            }
        });
    }

    [Fact]
    public async Task Scrape_Catalogue_Only()
    {
        const string targetUrl = "https://www.avito.ru/all/gruzoviki_i_spetstehnika/pogruzchiki-ASgBAgICAURU4E0";
        BrowserFactory factory = new BrowserFactory();
        IBrowser browser = await factory.ProvideBrowser(headless: false);
        IPage page = await browser.GetPage();
        await page.NavigatePage(targetUrl);
        bool solved = await new AvitoByPassFirewallWithRetry(new AvitoBypassFirewallLazy(page, new AvitoBypassFirewall(page))).Bypass();
        if (!solved) return;
        await page.ScrollBottom();
        await page.ScrollTop();
        AvitoPagination pagination = await AvitoPagination.FromCatalogue(page, targetUrl);
        await pagination.ProcessWhileNotReachedMaxPage(async () =>
        {
            solved = await new AvitoByPassFirewallWithRetry(new AvitoBypassFirewallLazy(page, new AvitoBypassFirewall(page))).Bypass();
            AvitoCatalogueItemsCollection items = await AvitoCatalogueItemsCollection.FromCatalogue(page);
            // save these items to database.
        });
    }
}