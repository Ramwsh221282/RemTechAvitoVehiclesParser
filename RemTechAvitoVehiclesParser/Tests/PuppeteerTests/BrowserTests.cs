using AvitoFirewallBypass;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.Parsing;

namespace Tests.PuppeteerTests;

public sealed class BrowserTests(PuppeteerTestFixture fixture) : IClassFixture<PuppeteerTestFixture>
{
    private readonly BrowserFactory _browserFactory = fixture.Services.GetRequiredService<BrowserFactory>();
    private readonly AvitoBypassFactory _bypassFactory = fixture.Services.GetRequiredService<AvitoBypassFactory>();

    [Fact]
    private async Task Scrape_Single_Catalogue_Item()
    {
        const string url =
            "https://www.avito.ru/vaskelovo/gruzoviki_i_spetstehnika/harvester_john_deere_1270d_4190912922?context=H4sIAAAAAAAA_wE_AMD_YToyOntzOjEzOiJsb2NhbFByaW9yaXR5IjtiOjA7czoxOiJ4IjtzOjE2OiI3c05LZmJnZFJONU1rYjB5Ijt9GqaZOT8AAAA";
        IBrowser browser = await _browserFactory.ProvideBrowser(headless: false);
        IPage page = await browser.GetPage();
        AvitoSpecialEquipmentAdvertisement advertisement = await AvitoSpecialEquipmentAdvertisement.Create(page, url, _bypassFactory);

        ITextTransformer transformer = new TextTransformerBuilder()
            .UsePunctuationCleaner()
            .UseNewLinesCleaner()
            .UseSpacesCleaner()
            .Build();

        Assert.True(await advertisement.HasTitle());
        Assert.True(await advertisement.HasPrice());
        Assert.True(await advertisement.HasCharacteristics());
        Assert.True(await advertisement.HasDescription(transformer));
        Assert.True(await advertisement.HasAddress(transformer));

        await browser.DestroyAsync();
    }

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