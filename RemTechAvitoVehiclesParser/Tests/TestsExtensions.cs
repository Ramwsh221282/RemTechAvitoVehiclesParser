using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests;

public static class TestsExtensions
{
    public static PostgreSqlContainer BuildPgVectorContainer(this PostgreSqlBuilder builder) =>
        builder
            .WithImage("pgvector/pgvector:0.8.0-pg17-bookworm")
            .WithDatabase("database")
            .WithUsername("username")
            .WithPassword("password")
            .Build();
    
    public static RabbitMqContainer BuildRabbitMqContainer(this RabbitMqBuilder builder) =>
        builder.WithImage("rabbitmq:3.11").Build();

    public static void ReconfigureDatabaseConfiguration(this IServiceCollection services, PostgreSqlContainer container)
    {
        services.RemoveAll<IOptions<NpgSqlOptions>>();
        string connectionString = container.GetConnectionString();
        string[] pairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, string> parameters = [];

        foreach (string pair in pairs)
        {
            string[] keyValuePair = pair.Split('=');
            string optionName = keyValuePair[0];
            string optionValue = keyValuePair[1];
            parameters.Add(optionName, optionValue);
        }

        string hostname = parameters["Host"];
        string port = parameters["Port"];
        string username = parameters["Username"];
        string password = parameters["Password"];
        string database = parameters["Database"];
        
        services.AddSingleton(Options.Create(new NpgSqlOptions()
        {
            Host = hostname,
            Port = port,
            Username = username,
            Password = password,
            Database = database,
        }));
    }
    
    public static void DontUseQuartzServices(this IServiceCollection services)
    {
        services.RemoveAll<ICronScheduleJob>();
        services.RemoveAll<IJob>();
    }
    
    public static void ReconfigureQuartzHostedService(this IServiceCollection services)
    {
        services.RemoveAll<QuartzHostedService>();
        services.RegisterQuartzJobs();
    }
    
    public static void ReconfigureRabbitMqConfiguration(this IServiceCollection services, RabbitMqContainer container)
    {
        services.RemoveAll<IOptions<RabbitMqConnectionOptions>>();
        string connectionString = container.GetConnectionString();
        string[] parts = connectionString.Split('@', StringSplitOptions.RemoveEmptyEntries);
        string[] hostParts = parts[1].Split(':', StringSplitOptions.RemoveEmptyEntries);
    
        string host = hostParts[0];
        string port = hostParts[1].Replace("/", string.Empty);
        string[] userParts = parts[0]
            .Split("//", StringSplitOptions.RemoveEmptyEntries)[1]
            .Split(':', StringSplitOptions.RemoveEmptyEntries);
        string username = userParts[0];
        string password = userParts[1];

        services.AddSingleton(Options.Create(new RabbitMqConnectionOptions()
        {
            Hostname = host,
            Port = int.Parse(port),
            Password = password,
            Username = username,
        }));
    }
}