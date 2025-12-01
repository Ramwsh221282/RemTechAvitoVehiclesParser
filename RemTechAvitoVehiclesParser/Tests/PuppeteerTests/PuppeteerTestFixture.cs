using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<BrowserInstance>();
        _sp = services.BuildServiceProvider();
    }
    
    public async Task InitializeAsync()
    {
        BrowserDownloader downloader = _sp.GetRequiredService<BrowserDownloader>();
        await downloader.DownloadBrowser();
        BrowserInstance instance = _sp.GetRequiredService<BrowserInstance>();
        await instance.Instantiate(headless: false);
    }

    public async Task DisposeAsync()
    {
        BrowserInstance instance = _sp.GetRequiredService<BrowserInstance>();
        await instance.Destroy();
    }
}