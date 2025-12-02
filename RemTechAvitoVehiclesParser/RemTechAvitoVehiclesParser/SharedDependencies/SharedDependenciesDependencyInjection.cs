using Quartz;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket.Decorators;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;
using Serilog;

namespace RemTechAvitoVehiclesParser.SharedDependencies;

public static class SharedDependenciesDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterSharedDependencies()
        {
            services.RegisterNpgSql();
            services.RegisterRabbitMq();
            services.RegisterLogger();
            services.RegisterQuartzJobs();
        }

        private void RegisterNpgSql()
        {
            services.AddOptions<NpgSqlOptions>().BindConfiguration(nameof(NpgSqlOptions));
            services.AddScoped<NpgSqlDataSourceFactory>();
            services.AddScoped<NpgSqlSession>();
            services.AddSingleton<DbUpgrader>();
        }

        private void RegisterRabbitMq()
        {
            services.AddOptions<RabbitMqConnectionOptions>().BindConfiguration(nameof(RabbitMqConnectionOptions));
            services.AddSingleton<RabbitMqConnectionFactory>();
        }

        public void RegisterQuartzJobs()
        {
            services.AddCronScheduledJobs();
            services.AddQuartzHostedService(c =>
            {
                c.AwaitApplicationStarted = true;
                c.WaitForJobsToComplete = true;
            });
        }
        
        private void RegisterLogger()
        {
            services.AddSingleton<Serilog.ILogger>(new LoggerConfiguration().WriteTo.Console().CreateLogger());
        }
    }

    extension(IServiceProvider sp)
    {
        public void ApplyDatabaseMigrations()
        {
            DbUpgrader upgrader = sp.GetRequiredService<DbUpgrader>();
            upgrader.UpgradeDatabase();
        }

        public async Task RequireParserRegistration(string domain, string type)
        {
            await using AsyncServiceScope scope = sp.CreateAsyncScope();
            RegisterParserCreationTicketCommand command = new(domain, type);
            IRegisterParserCreationTicket registration = scope.ServiceProvider.GetRequiredService<IRegisterParserCreationTicket>();
            await registration.Handle(command);
        }
    }
}