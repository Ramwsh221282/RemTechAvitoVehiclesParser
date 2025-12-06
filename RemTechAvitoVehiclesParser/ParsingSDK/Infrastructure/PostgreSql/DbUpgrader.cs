using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Options;

namespace RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

public sealed class DbUpgrader
{
    private readonly IOptions<NpgSqlOptions> _options;
    
    public DbUpgrader(IOptions<NpgSqlOptions> options) => _options = options;
    
    public void UpgradeDatabase()
    {
        UpgradeEngine engine = DeployChanges.To.PostgresqlDatabase(_options.Value.ToConnectionString())
            .WithScriptsEmbeddedInAssembly(typeof(DbUpgrader).Assembly)
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = engine.PerformUpgrade();
        if (!result.Successful)
            throw new ApplicationException($"Unable to apply database migrations. Error: {result.Error.Message}");
    }
}