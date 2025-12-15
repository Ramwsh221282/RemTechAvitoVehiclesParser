using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;
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

    extension(IServiceProvider sp)
    {
        public async Task RequireParserRegistration(string domain, string type)
        {
            await using AsyncServiceScope scope = sp.CreateAsyncScope();
            RegisterParserCreationTicketCommand command = new(domain, type);
            IRegisterParserCreationTicket registration = scope.ServiceProvider.GetRequiredService<IRegisterParserCreationTicket>();
            await registration.Handle(command);
        }
    }
}