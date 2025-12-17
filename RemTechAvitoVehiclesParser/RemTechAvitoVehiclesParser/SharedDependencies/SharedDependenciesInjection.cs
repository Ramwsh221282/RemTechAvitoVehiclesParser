using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

namespace RemTechAvitoVehiclesParser.SharedDependencies;

public static class SharedDependenciesInjection
{
    extension(IServiceCollection services)
    {
        public void AddDbUpgrader()
        {
            services.AddTransient<IDbUpgrader, RemTechAvitoParserDbUpgrader>();
        }
    }
}