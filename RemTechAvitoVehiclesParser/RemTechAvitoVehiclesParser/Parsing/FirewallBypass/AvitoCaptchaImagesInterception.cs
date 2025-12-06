using ParsingSDK;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;

namespace RemTechAvitoVehiclesParser.Parsing.FirewallBypass;

public sealed class AvitoCaptchaImagesInterception
{
    private readonly Maybe<IElementHandle> _blockedForm;
    private readonly IPage _page;
    private readonly TaskCompletionSource _tcs;
    private AvitoCaptchaImages _images;

    public AvitoCaptchaImagesInterception(IPage page, Maybe<IElementHandle> blockedForm)
    {
        _tcs = new TaskCompletionSource();
        _page = page;
        _blockedForm = blockedForm;
        _images = new AvitoCaptchaImages();
    }

    public async Task<AvitoCaptchaImages> Intercept()
    {
        if (!_blockedForm.HasValue)
            return _images;

        _page.Response += InterceptionMethod!;
        Maybe<IElementHandle> button = await _page.GetElementRetriable("button", retryAmount: 10);
        if (!button.HasValue)
        {
            _page.Response -= InterceptionMethod!;
            return _images;
        }
        
        await button.Value.ClickAsync();
        Task imagesTask = _tcs.Task;
        Task timeOutTask = Task.Delay(TimeSpan.FromSeconds(10));
        Task completed = await Task.WhenAny(imagesTask, timeOutTask);
        if (completed == imagesTask)
            _tcs.TrySetResult();
        
        _page.Response -= InterceptionMethod!;
        await button.Value.DisposeAsync();
        return _images;
    }

    private async void InterceptionMethod(object sender, ResponseCreatedEventArgs ea)
    {
        try
        {
            string requestUrl = ea.Response.Request.Url;
            if (requestUrl.Contains("/bg/") || requestUrl.Contains("/slide/"))
            {
                await ea
                    .Response.BufferAsync()
                    .AsTask()
                    .ContinueWith(async data => _images = _images.With(await data));
            }
            
            if (_images.Filled()) _tcs.TrySetResult();
        }
        catch
        {
            _images = new  AvitoCaptchaImages();
        }
    }
}