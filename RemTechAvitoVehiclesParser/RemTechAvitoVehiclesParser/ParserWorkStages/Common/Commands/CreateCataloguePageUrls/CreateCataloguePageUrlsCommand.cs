using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Extensions;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.CreateCataloguePageUrls;

public sealed class CreateCataloguePageUrlsCommand(Func<Task<IPage>> pageSource, string url, AvitoBypassFactory bypassFactory) : ICreateCataloguePageUrlsCommand
{
    public async Task<CataloguePageUrl[]> Handle()
    {
        const string javaScript = @"
                                   () => {
                                   const currentUrl = window.location.href;
                                   const paginationSelector = document.querySelector('nav[aria-label=""Пагинация""]');
                                   if (!paginationSelector) return [currentUrl];
                                   const paginationGroupSelector = paginationSelector.querySelector('ul[data-marker=""pagination-button""]');
                                   if (!paginationGroupSelector) return [currentUrl];
                                   return Array.from(paginationGroupSelector.querySelectorAll('span[class=""styles-module-text-Z0vDE""]'))
                                               .map(s => s.innerText)
                                               .map(p => currentUrl + '&p=' + p);
                                   }
                                   ";
        
        IPage page = await pageSource();
        await page.PerformQuickNavigation(url);
        if (!await bypassFactory.Create(page).Bypass()) 
            throw new InvalidOperationException("Unable to bypass Avito firewall");
        
        await page.ResilientWaitForSelector("nav[aria-label='Пагинация']");
        string[] urls = await page.EvaluateFunctionAsync<string[]>(javaScript);
        return urls.Select(CataloguePageUrl.New).ToArray();
    }
}