using PuppeteerSharp;
using RemTechAvitoVehiclesParser;
using RemTechAvitoVehiclesParser.Utils;

namespace Tests.PuppeteerTests;

public sealed class DisposableResourcesManager
{
    private readonly Queue<IDisposable> _disposables = [];
    private readonly Queue<IAsyncDisposable> _asyncDisposables = [];

    public DisposableResourcesManager Add(IDisposable disposable)
    {
        _disposables.Enqueue(disposable);
        return this;
    }

    public DisposableResourcesManager Add(IElementHandle disposable)
    {
        IJSHandle jsHandle = disposable;
        _asyncDisposables.Enqueue(jsHandle);
        return this;
    }

    public DisposableResourcesManager Add(Maybe<IElementHandle> disposable)
    {
        if (disposable.HasValue) _asyncDisposables.Enqueue(disposable.Value);
        return this;
    }

    public DisposableResourcesManager Add(IEnumerable<IElementHandle> disposables)
    {
        foreach (IElementHandle element in disposables)
        {
            IJSHandle jsHandle = element;
            _asyncDisposables.Enqueue(jsHandle);
        }

        return this;
    }

    public async Task DisposeResources()
    {
        foreach (IDisposable disposable in _disposables)
            disposable.Dispose();
        foreach (IAsyncDisposable asyncDisposable in _asyncDisposables)
            await asyncDisposable.DisposeAsync();
    }
}