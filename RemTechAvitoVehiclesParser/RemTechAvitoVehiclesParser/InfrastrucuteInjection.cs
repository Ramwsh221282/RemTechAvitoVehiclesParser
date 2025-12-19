using RemTech.SharedKernel.Infrastructure;
using RemTechAvitoVehiclesParser.SharedDependencies;

namespace RemTechAvitoVehiclesParser;

public static class InfrastrucuteInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterInfrastructureDependencies()
        {
            services.AddDbUpgrader();
            services.RegisterSharedInfrastructure();
            services.AddQuartzServices();
        }
    }
}