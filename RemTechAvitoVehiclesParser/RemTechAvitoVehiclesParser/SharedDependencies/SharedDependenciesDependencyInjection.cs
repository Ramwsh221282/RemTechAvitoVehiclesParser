using ParsingSDK.Infrastructure.PostgreSql;
using Quartz;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace RemTechAvitoVehiclesParser.SharedDependencies;

public sealed class ClassNameLogEnricher : ILogEventEnricher
{
    private const string Pattern = "SourceContext";
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue(Pattern, out var sourceContext))
        {
            string fullName = sourceContext.ToString().Trim('\"');
            string exactTypeName = fullName.Split('.').Last();
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(Pattern, exactTypeName));
        }
    }
}

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
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            services.AddOptions<NpgSqlOptions>().BindConfiguration(nameof(NpgSqlOptions));
            services.AddSingleton<NpgSqlDataSourceFactory>();
            services.AddScoped<IPostgreSqlAdapter, NpgSqlSession>();
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
            Serilog.ILogger logger = new LoggerConfiguration()
                .Enrich.With(new ClassNameLogEnricher())
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message}{NewLine}{Exception}")
                .CreateLogger();
            services.AddSingleton(logger);
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