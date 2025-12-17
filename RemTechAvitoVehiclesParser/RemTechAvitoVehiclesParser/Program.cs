using AvitoFirewallBypass;
using Microsoft.Extensions.Options;
using ParsingSDK;
using ParsingSDK.TextProcessing;
using RemTech.SharedKernel.Infrastructure;
using RemTechAvitoVehiclesParser;
using RemTechAvitoVehiclesParser.ParserWorkStages;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbUpgrader();
builder.Services.RegisterTextTransformerBuilder();
builder.Services.RegisterParserWorkStagesContext();
builder.Services.RegisterParserDependencies(conf =>
{
    IOptions<ScrapingBrowserOptions> options = Options.Create(new ScrapingBrowserOptions()
    {
        Headless = false,
        BrowserPath = "C:\\Users\\ramwsh\\Desktop\\avito_vehicles_parser\\RemTechAvitoVehiclesParser\\RemTechAvitoVehiclesParser\\Tests\\bin\\Debug\\net10.0\\Chromium\\Win64-1559811\\chrome-win\\chrome.exe"
    });
    conf.AddSingleton(options);
});
builder.Services.RegisterAvitoParsing();
builder.Services.RegisterAvitoFirewallBypass();
builder.Services.RegisterSharedInfrastructure();
builder.Services.RegisterParserSubscription();
builder.Services.AddQuartzServices();

WebApplication app = builder.Build();

app.Services.ApplyDatabaseMigrations();

app.Run();

namespace RemTechAvitoVehiclesParser
{
    public partial class Program { }
}
