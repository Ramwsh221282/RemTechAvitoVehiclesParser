using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests.ParserServiceRegistrationTests;

public sealed class ParserServiceRegistrationFixture : WebApplicationFactory<RemTechAvitoVehiclesParser.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().BuildPgVectorContainer();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().BuildRabbitMqContainer();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(s =>
        {
            s.ReconfigureDatabaseConfiguration(_dbContainer);
            s.ReconfigureRabbitMqConfiguration(_rabbitMq);
            s.AddHostedService<TestParserRegistrationTicketApprovalService>();
            s.AddHostedService<TestConfirmPendingRegistrationTicketService>();
            s.AddScoped<PublisherToParserRegistrationTicketApproval>();
            s.ReconfigureQuartzHostedService();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMq.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _rabbitMq.StopAsync();
        await _dbContainer.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}