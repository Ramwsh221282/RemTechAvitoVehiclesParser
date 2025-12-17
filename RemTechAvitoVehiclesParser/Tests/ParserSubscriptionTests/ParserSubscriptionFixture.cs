using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RemTech.SharedKernel.Infrastructure;
using RemTech.Tests.Shared;
using RemTechAvitoVehiclesParser;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests.ParserSubscriptionTests;

public sealed class ParserSubscriptionFixture : WebApplicationFactory<RemTechAvitoVehiclesParser.Program>, IAsyncLifetime
{
    private PostgreSqlContainer DbContainer { get; } = new PostgreSqlBuilder().BuildPgVectorContainer();
    private RabbitMqContainer BrokerContainer { get; } = new RabbitMqBuilder().BuildRabbitMqContainer();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(s =>
        {
            s.ReconfigureConfigurationProvider();
            s.ReconfigurePostgreSqlOptions(DbContainer);
            s.ReconfigureRabbitMqOptions(BrokerContainer);
            s.AddHostedService<FakeParserSubscriptionQueue>();
            s.AddTransient<FakeParserSubscriptionQueuePublisher>();
            s.RegisterParserSubscription();
        });
    }

    public async Task InitializeAsync()
    {
        await DbContainer.StartAsync();
        await BrokerContainer.StartAsync();
        Services.ApplyDatabaseMigrations();
    }

    public async Task DisposeAsync()
    {
        await BrokerContainer.StopAsync();
        await DbContainer.StopAsync();
        await BrokerContainer.DisposeAsync();
        await DbContainer.DisposeAsync();
    }
}