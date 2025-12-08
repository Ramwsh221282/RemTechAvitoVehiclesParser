using ParsingSDK;
using ParsingSDK.TextProcessing;
using RemTech.SharedKernel.Infrastructure;
using RemTechAvitoVehiclesParser.ParserServiceRegistration;
using RemTechAvitoVehiclesParser.ParserWorkStages;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.ResultsPublishing;
using RemTechAvitoVehiclesParser.SharedDependencies;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbUpgrader();
builder.Services.RegisterTextTransformerBuilder();
builder.Services.RegisterParserServiceRegistrationContext();
builder.Services.RegisterParserWorkStagesContext();
builder.Services.RegisterParserDependencies();
builder.Services.RegisterAvitoParsing();
builder.Services.RegisterResultPublishing();
builder.Services.RegisterSharedInfrastructure();

WebApplication app = builder.Build();

app.Services.ApplyDatabaseMigrations();

await app.Services.RequireParserRegistration(
    ConstantsForMainApplicationCommunication.CurrentServiceDomain, 
    ConstantsForMainApplicationCommunication.CurrentServiceType);

app.Run();

namespace RemTechAvitoVehiclesParser
{
    public partial class Program
    {
    
    }
}