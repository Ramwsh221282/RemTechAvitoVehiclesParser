using RemTechAvitoVehiclesParser.Configuration;
using RemTechAvitoVehiclesParser.Database;
using RemTechAvitoVehiclesParser.OnStartup.RegisterParser;
using RemTechAvitoVehiclesParser.OnStartup.RegisterParser.Decorators;
using RemTechAvitoVehiclesParser.RabbitMq;
using Serilog;

namespace RemTechAvitoVehiclesParser.DependencyInjection;

public static class AvitoParserDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterDependencies()
        {
            services.AddDatabaseDependencies();
            services.AddRabbitMqDependencies();
            services.AddRegisterParserOnStartup();
            services.AddLogger();
        }

        private void AddRegisterParserOnStartup()
        {
            services.AddScoped<IRegisterParserOnStartup, RegisterParserOnStartup>();
            services.Decorate<IRegisterParserOnStartup, TransactionalRegisterParserOnStartup>();
            services.Decorate<IRegisterParserOnStartup, LoggingRegisterParserOnStartup>();
        }

        private void AddLogger()
        {
            services.AddSingleton<Serilog.ILogger>(new LoggerConfiguration().WriteTo.Console().CreateLogger());
        }
        
        private void AddDatabaseDependencies()
        {
            services.AddOptions<NpgSqlOptions>().BindConfiguration(nameof(NpgSqlOptions));
            services.AddSingleton<NpgSqlDataSourceContainer>();
            services.AddScoped<NpgSqlSession>();
            services.AddScoped<NpgSqlParserTicketsStorage>();
            services.AddSingleton<DbUpgrader>();
        }

        private void AddRabbitMqDependencies()
        {
            services.AddOptions<RabbitMqConnectionOptions>()
                .BindConfiguration(nameof(RabbitMqConnectionOptions));
            
            services.AddSingleton<RabbitMqConnectionSource>();
        }
    }
}