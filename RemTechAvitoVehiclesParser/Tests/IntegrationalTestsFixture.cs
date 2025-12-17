using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RemTech.SharedKernel.Infrastructure;
using RemTech.Tests.Shared;
using RemTechAvitoVehiclesParser;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Tests.ParserStagesTests;

namespace Tests;

public sealed class IntegrationalTestsFixture : WebApplicationFactory<RemTechAvitoVehiclesParser.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().BuildPgVectorContainer();
    private readonly RabbitMqContainer _brokerContainer = new RabbitMqBuilder().BuildRabbitMqContainer();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(s =>
        {
            s.ReconfigureConfigurationProvider();
            s.ReconfigurePostgreSqlOptions(_dbContainer);
            s.ReconfigureRabbitMqOptions(_brokerContainer);
            s.ReconfigureQuartzHostedService();
            s.RegisterParserSubscription();
            s.AddTransient<FakeStartParserPublisher>();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _brokerContainer.StartAsync();
        Services.ApplyDatabaseMigrations();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _brokerContainer.StopAsync();
        await _brokerContainer.DisposeAsync();
    }    
}