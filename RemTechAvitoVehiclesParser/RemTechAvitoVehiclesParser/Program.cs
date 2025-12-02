using RemTechAvitoVehiclesParser.Constants;
using RemTechAvitoVehiclesParser.DependencyInjection;
using RemTechAvitoVehiclesParser.OnStartup.ApplyDatabaseMigrations;
using RemTechAvitoVehiclesParser.OnStartup.RegisterParser;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterDependencies();

WebApplication app = builder.Build();
app.ApplyMigrations();
await app.RegisterParser(ParserServiceConstants.Domain, ParserServiceConstants.Type);

app.Run();