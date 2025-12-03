using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RemTechAvitoVehiclesParser.SharedDependencies;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests.ParserWorkStartTests;

public sealed class ParserWorkStartFixture : WebApplicationFactory<RemTechAvitoVehiclesParser.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().BuildPgVectorContainer();
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder().BuildRabbitMqContainer();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(s =>
        {
            s.ReconfigureDatabaseConfiguration(_dbContainer);
            s.ReconfigureRabbitMqConfiguration(_rabbitMqContainer);
            s.AddScoped<TestStartParserWorkPublisher>();
            s.DontUseQuartzServices();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        Services.ApplyDatabaseMigrations();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _rabbitMqContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}