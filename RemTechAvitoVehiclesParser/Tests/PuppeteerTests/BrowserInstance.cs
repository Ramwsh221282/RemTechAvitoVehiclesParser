using PuppeteerExtraSharp;
using PuppeteerExtraSharp.Plugins.BlockResources;
using PuppeteerExtraSharp.Plugins.ExtraStealth;
using PuppeteerSharp;

namespace Tests.PuppeteerTests;

public sealed class BrowserInstance
{
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private bool _instantiated;

    public BrowserInstance()
    {
    }

    public async Task Invoke(Func<IBrowser, Task> invoke)
    {
        await invoke(_browser);
    }

    public async Task Invoke(Func<IPage, Task> invoke)
    {
        await invoke(_page);
    }

    public async Task<U> Invoke<U>(Func<IPage, Task<U>> invoke)
    {
        return await invoke(_page);
    }
    
    public async Task<U> Invoke<U>(Func<IBrowser, Task<U>> invoke)
    {
        return await invoke(_browser);
    }

    public async Task Destroy()
    {
        await _page.DisposeAsync();
        await _browser.CloseAsync();
    }
    
    public async Task Instantiate(bool headless = true)
    {
        if (_instantiated) return;
        
        LaunchOptions options = new LaunchOptions
        {
            Headless = headless
        };
        
        PuppeteerExtra extra = new PuppeteerExtra();
        extra.Use(new BlockResourcesPlugin()).Use(new StealthPlugin());
        _browser = await extra.LaunchAsync(options);
        IPage[]? pages = await _browser.PagesAsync();
        _page = pages.First();
        _instantiated = true;
    }
}