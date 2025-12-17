using AvitoFirewallBypass;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.Common;
using RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.CreateCataloguePageUrls;
using RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractCatalogueItemData;
using RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractConcreteItem;
using RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.PrepareAvitoPage;

namespace Tests.ParsingTests;

public sealed class AvitoParsingTests(IntegrationalTestsFixture fixture) : IClassFixture<IntegrationalTestsFixture>
{
    private BrowserFactory BrowserFactory { get; } = fixture.Services.GetRequiredService<BrowserFactory>();
    private AvitoBypassFactory AvitoBypassFactory { get; } = fixture.Services.GetRequiredService<AvitoBypassFactory>();
    
    const string targetUrl = "https://www.avito.ru/all/gruzoviki_i_spetstehnika/tehnika_dlya_lesozagotovki/john_deere-ASgBAgICAkRUsiyexw3W6j8?cd=1";
    
    [Fact]
    private async Task Test_Extract_Catalogue_Items()
    {
        IBrowser browser = await BrowserFactory.ProvideBrowser();
        IPage page = await browser.GetPage();
        await page.PerformQuickNavigation(targetUrl);
        
        await new PrepareAvitoPageCommand(() => Task.FromResult(page), AvitoBypassFactory).Handle();
        CataloguePageUrl[] pagedUrls = await new CreateCataloguePageUrlsCommand(() => Task.FromResult(page), targetUrl, AvitoBypassFactory).Handle();
        
        List<AvitoVehicle> results = [];
        for (int i = 0; i < pagedUrls.Length; i++)
        {
            CataloguePageUrl url = pagedUrls[i];
            await page.PerformQuickNavigation(url.Url);
            await new PrepareAvitoPageCommand(() => Task.FromResult(page), AvitoBypassFactory).Handle();
            AvitoVehicle[] data = await new ExtractCatalogueItemDataCommand(() => Task.FromResult(page), url, AvitoBypassFactory).Handle();
            results.AddRange(data);
        }
        
        await browser.DestroyAsync();
        Assert.NotEmpty(results);
    }

    [Fact]
    private async Task Test_Extract_Pagination_Urls()
    {
        IBrowser browser = await BrowserFactory.ProvideBrowser();
        IPage page = await browser.GetPage();
        await page.PerformQuickNavigation(targetUrl);
        
        await new PrepareAvitoPageCommand(() => Task.FromResult(page), AvitoBypassFactory).Handle();
        CataloguePageUrl[] pagedUrls = await new CreateCataloguePageUrlsCommand(() => Task.FromResult(page), targetUrl, AvitoBypassFactory).Handle();
        await browser.DestroyAsync();
        Assert.NotEmpty(pagedUrls);
    }
    

    [Fact]
    private async Task Parse_Concrete_Items()
    {
        IBrowser browser = await BrowserFactory.ProvideBrowser();
        IPage page = await browser.GetPage();
        await page.PerformQuickNavigation(targetUrl);
        
        await new PrepareAvitoPageCommand(() => Task.FromResult(page), AvitoBypassFactory).Handle();
        CataloguePageUrl[] pagedUrls = await new CreateCataloguePageUrlsCommand(() => Task.FromResult(page), targetUrl, AvitoBypassFactory).Handle();
        List<AvitoVehicle> results = [];
        for (int i = 0; i < pagedUrls.Length; i++)
        {
            CataloguePageUrl url = pagedUrls[i];
            await page.PerformQuickNavigation(url.Url);
            await new PrepareAvitoPageCommand(() => Task.FromResult(page), AvitoBypassFactory).Handle();
            AvitoVehicle[] data = await new ExtractCatalogueItemDataCommand(() => Task.FromResult(page), url, AvitoBypassFactory).Handle();
            for (int j = 0; j < data.Length; j++)
            {
                AvitoVehicle vehicle = data[j];
                vehicle = await new ExtractConcreteItemCommand(() => Task.FromResult(page), vehicle, AvitoBypassFactory).Handle();
                data[j] = vehicle;
            }
            
            results.AddRange(data);
        }

        AvitoVehicle[] processed = results.Where(v => v.ConcretePageRepresentation.IsEmpty() == false).ToArray();
        await browser.DestroyAsync();
        Assert.NotEmpty(processed);
    }
}