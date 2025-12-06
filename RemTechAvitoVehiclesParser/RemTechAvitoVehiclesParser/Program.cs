using ParsingSDK;
using RemTechAvitoVehiclesParser.ParserServiceRegistration;
using RemTechAvitoVehiclesParser.ParserWorkStages;
using RemTechAvitoVehiclesParser.Parsing;
using RemTechAvitoVehiclesParser.ResultsPublishing;
using RemTechAvitoVehiclesParser.SharedDependencies;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;
using RemTechAvitoVehiclesParser.Utilities;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterUtilities();
builder.Services.RegisterParserServiceRegistrationContext();
builder.Services.RegisterParserWorkStagesContext();
builder.Services.RegisterParserDependencies();
builder.Services.RegisterAvitoParsing();
builder.Services.RegisterResultPublishing();
builder.Services.RegisterSharedDependencies();

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