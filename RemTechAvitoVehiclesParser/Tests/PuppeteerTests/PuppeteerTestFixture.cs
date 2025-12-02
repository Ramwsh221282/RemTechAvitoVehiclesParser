using Microsoft.Extensions.DependencyInjection;
using RemTechAvitoVehiclesParser;
using RemTechAvitoVehiclesParser.Parsing;
using Serilog;

namespace Tests.PuppeteerTests;

public sealed class PuppeteerTestFixture : IAsyncLifetime
{
    private readonly IServiceProvider _sp;

    public IServiceProvider Services => _sp;
    
    public PuppeteerTestFixture()
    {
        IServiceCollection services = new ServiceCollection();
        ILogger logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        services.AddSingleton(logger);
        services.AddSingleton<BrowserDownloader>();
        services.AddSingleton<BrowserFactory>();
        _sp = services.BuildServiceProvider();
    }
    
    public async Task InitializeAsync()
    {
        BrowserDownloader downloader = _sp.GetRequiredService<BrowserDownloader>();
        await downloader.DownloadBrowser();
    }

    public async Task DisposeAsync()
    {
        await Task.Yield();
    }
}