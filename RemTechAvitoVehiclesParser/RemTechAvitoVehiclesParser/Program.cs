using RemTechAvitoVehiclesParser.ParserServiceRegistration;
using RemTechAvitoVehiclesParser.SharedDependencies;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterParserServiceRegistrationContext();
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