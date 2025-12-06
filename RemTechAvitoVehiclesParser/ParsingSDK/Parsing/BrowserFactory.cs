using PuppeteerSharp;

namespace ParsingSDK.Parsing;

public sealed class BrowserFactory
{
    public async Task<IBrowser> ProvideBrowser(bool headless = true)
    {
        LaunchOptions options = new LaunchOptions
        {
            Headless = headless,
            Args = 
            [
                "--no-sandbox",
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--no-zygote",
            ],
            UserDataDir = null
        };

        IBrowser browser = await Puppeteer.LaunchAsync(options);
        browser.DefaultWaitForTimeout = 0;
        return browser;
    }
}