using RemTech.SharedKernel.Infrastructure;
using RemTechAvitoVehiclesParser;
using RemTechAvitoVehiclesParser.Parsing.BackgroundTasks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterDependenciesForParsing();
builder.Services.RegisterAvitoStartQueue();
builder.Services.RegisterInfrastructureDependencies();
WebApplication app = builder.Build();
app.Services.ApplyDatabaseMigrations();
app.Run();

namespace RemTechAvitoVehiclesParser
{
    public partial class Program { }
}
