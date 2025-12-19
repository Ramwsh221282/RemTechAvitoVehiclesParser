using AvitoFirewallBypass;
using Microsoft.Extensions.Options;
using ParsingSDK;
using ParsingSDK.TextProcessing;
using RemTechAvitoVehiclesParser.ParserWorkStages;

namespace RemTechAvitoVehiclesParser;

public static class ParserDependenciesInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterDependenciesForParsing()
        {
            services.RegisterParserWorkStagesContext();
            services.RegisterParserSubscription();
            services.RegisterTextTransformerBuilder();
            services.RegisterAvitoFirewallBypass();
            services.RegisterParserDependencies(conf =>
            {
                IOptions<ScrapingBrowserOptions> options = Options.Create(new ScrapingBrowserOptions()
                {
                    Headless = false,
                    BrowserPath = "C:\\Users\\ramwsh\\Desktop\\avito_vehicles_parser\\RemTechAvitoVehiclesParser\\RemTechAvitoVehiclesParser\\Tests\\bin\\Debug\\net10.0\\Chromium\\Win64-1559811\\chrome-win\\chrome.exe"
                });
                conf.AddSingleton(options);
            });
        }
    }
}