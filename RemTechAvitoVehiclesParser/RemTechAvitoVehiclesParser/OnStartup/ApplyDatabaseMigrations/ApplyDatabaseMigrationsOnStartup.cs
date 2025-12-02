using RemTechAvitoVehiclesParser.Database;

namespace RemTechAvitoVehiclesParser.OnStartup.ApplyDatabaseMigrations;

public static class ApplyDatabaseMigrationsOnStartup
{
    extension(WebApplication app)
    {
        public void ApplyMigrations()
        {
            IServiceProvider provider = app.Services;
            DbUpgrader upgrader = provider.GetRequiredService<DbUpgrader>();
            upgrader.UpgradeDatabase();
        }
    }
}