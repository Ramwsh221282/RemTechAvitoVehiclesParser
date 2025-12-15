using PuppeteerSharp;
using PuppeteerSharp.BrowserData;

namespace Tests.PuppeteerTests;

public sealed class BrowserDownloader
{
    private readonly Serilog.ILogger _logger;

    public BrowserDownloader(Serilog.ILogger logger)
    {
        _logger = logger;
    }

    public async Task DownloadBrowser()
    {
        _logger.Information("Loading browser...");

        BrowserFetcher fetcher = new BrowserFetcher();
        InstalledBrowser browser = await fetcher.DownloadAsync();
        _logger.Information("Loaded browser. Path: {Path}", browser.GetExecutablePath());
    }
}