using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;

namespace Tests.PuppeteerTests;

public sealed class BrowserTests(PuppeteerTestFixture fixture) : IClassFixture<PuppeteerTestFixture>
{
    private readonly PuppeteerTestFixture _fixture = fixture;

    [Fact]
    private async Task Get_Instantiated_Browser_Success()
    {
        IServiceProvider sp = _fixture.Services;
        BrowserInstance instance = sp.GetRequiredService<BrowserInstance>();
        await instance.Invoke(async page => await page.GoToAsync("https://www.google.com"));
        await Task.Delay(TimeSpan.FromSeconds(10));
    }
    
    [Fact]
    private async Task Scrape_Avito_Pagination()
    {
        const string targetUrl =
            "https://www.avito.ru/all/gruzoviki_i_spetstehnika/tehnika_dlya_lesozagotovki/john_deere-ASgBAgICAkRUsiyexw3W6j8";
        
        IServiceProvider sp = _fixture.Services;
        BrowserInstance instance = sp.GetRequiredService<BrowserInstance>();
        await instance.SetPageTo(targetUrl);
        await instance.ScrollBottom();
        await instance.ScrollTop();
        AvitoPagination pagination = await AvitoPagination.FromCataloguePage(instance);
        Assert.NotEqual(0, pagination.CurrentPageValue);
        Assert.NotEqual(0, pagination.MaxPageValue);
    }

    [Fact]
    private async Task Hover_Avito_Advertisement_Photos()
    {
        const string targetUrl =
            "https://www.avito.ru/all/gruzoviki_i_spetstehnika/tehnika_dlya_lesozagotovki/john_deere-ASgBAgICAkRUsiyexw3W6j8";
        
        IServiceProvider sp = _fixture.Services;
        BrowserInstance instance = sp.GetRequiredService<BrowserInstance>();
        await instance.SetPageTo(targetUrl);
        await instance.ScrollBottom();
        await instance.ScrollTop();
        // get catalogue container
        Maybe<IElementHandle> element = await instance.GetElement("div.index-root-H81wX");
        Assert.True(element.HasValue);
        // get catalogue advertisements
        IElementHandle[] advertisements = await element.Value.GetElements("div[data-marker='item']");
        
        // loop and hover in each advertisement photo container
        foreach (IElementHandle advertisement in advertisements)
        {
            Maybe<IElementHandle> itemImage = await advertisement
                .GetElement("div[data-marker='item-image']");
            if (!itemImage.HasValue) continue;
            
        }
    }
}

public sealed class SelectedAvitoPage
{
    public int Value { get; }

    private SelectedAvitoPage(int value)
    {
        Value = value;
    }

    public static async Task<SelectedAvitoPage> FromPaginationElements(IElementHandle[] elements)
    {
        int currentPage = 0;
        foreach (IElementHandle paginationElement in elements)
        {
            Maybe<IElementHandle> selectedPage = await paginationElement
                .GetElement("span[aria-current='page']");
            if (!selectedPage.HasValue) continue;
            
            Maybe<IElementHandle> pageNumberElement = await selectedPage
                .Value.GetElement("span.styles-module-text-Z0vDE");
            if (!pageNumberElement.HasValue) continue;
            
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue) continue;
            currentPage = int.Parse(pageNumberText.Value);
            break;
        }

        return new SelectedAvitoPage(currentPage);
    }
}

public sealed class MaxAvitoPage
{
    public int Value { get; }

    private MaxAvitoPage(int value)
    {
        Value = value;
    }

    public static async Task<MaxAvitoPage> FromPaginationElements(IElementHandle[] elements)
    {
        int maxPage = 0;
        foreach (IElementHandle pageElements in elements)
        {
            Maybe<IElementHandle> pageNumberElement = await pageElements.GetElement("span.styles-module-text-Z0vDE");
            if (!pageNumberElement.HasValue) continue;
            Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
            if (!pageNumberText.HasValue) continue;
            int somePage = int.Parse(pageNumberText.Value);
            if (maxPage < somePage)
                maxPage = somePage;
        }
        
        return new MaxAvitoPage(maxPage);
    }
}

public sealed class AvitoPagination
{
    public int CurrentPageValue { get; }
    public int MaxPageValue { get; }

    private AvitoPagination(int currentPage, int maxPage)
    {
        CurrentPageValue = currentPage;
        MaxPageValue = maxPage;
    }
    
    public static async Task<AvitoPagination> FromCataloguePage(BrowserInstance instance)
    {
        Maybe<IElementHandle> element = await instance.GetElement("nav[aria-label='Пагинация']");
        if (!element.HasValue) return new AvitoPagination(0, 0);
        IElementHandle[] paginationElements = await element.Value.GetElements("li");
        if (paginationElements.Length == 0) return new AvitoPagination(0, 0);
        SelectedAvitoPage current = await SelectedAvitoPage.FromPaginationElements(paginationElements);
        MaxAvitoPage maxPage = await MaxAvitoPage.FromPaginationElements(paginationElements);
        return new AvitoPagination(current.Value, maxPage.Value);
    }
}

public sealed class Maybe<T> where T : notnull
{
    public bool HasValue { get; }
    public T Value => HasValue
        ? field
        : throw new InvalidOperationException($"Cannot access none value of {nameof(Maybe<>)}");

    private Maybe(T value)
    {
        Value = value;
        HasValue = true;
    }

    private Maybe()
    {
        Value = default!;
        HasValue = false;
    }
    
    public static Maybe<T> Some(T value) => new(value);
    public static Maybe<T> None() => new();
    public static Maybe<T> Resolve(T? value) => value == null ? None() : Some(value);
    
    public static async Task<Maybe<T>> Resolve(Func<Task<T?>> fn)
    {
        T? value = await fn();
        return Resolve(value);
    }
}

public static class BrowserActions
{
    extension(IElementHandle element)
    {
        public async Task<Maybe<string>> GetElementInnerText()
        {
            string? text = await element.EvaluateFunctionAsync<string?>("el => el.innerText");
            return Maybe<string>.Resolve(text);
        }
        
        public async Task<IElementHandle[]> GetElements(string selectorQuery)
        {
            IElementHandle[] elements = await element.QuerySelectorAllAsync(selectorQuery);
            return elements;
        }

        public async Task<Maybe<IElementHandle>> GetElement(string selectorQuery)
        {
            return await Maybe<IElementHandle>.Resolve(async () => await element.QuerySelectorAsync(selectorQuery));
        }
    }
    
    extension(BrowserInstance instance)
    {
        public async Task SetPageTo(string url)
        {
            await instance.Invoke(async page =>
            {
                WaitUntilNavigation waitUntil = WaitUntilNavigation.Load;
                try
                {
                    await page.GoToAsync(url, waitUntil);
                }
                catch(NavigationException)
                {
                    Console.WriteLine($"Puppeteer timeout navigation exceeded.");
                }
            });
        }
        
        public async Task<Maybe<IElementHandle>> GetElement(string selectorQuery)
        {
            return await instance.Invoke(async (IPage page) =>
            {
                return await Maybe<IElementHandle>.Resolve(() => page.QuerySelectorAsync(selectorQuery));
            });
        }
        
        public async Task ScrollBottom()
        {
            await instance.Invoke(async (IPage page) =>
            {
                await page.EvaluateExpressionAsync("window.scrollBy(0, document.documentElement.scrollHeight)");
            });
        }

        public async Task ScrollTop()
        {
            await instance.Invoke(async (IPage page) =>
            {
                await page.EvaluateExpressionAsync("window.scrollTo(0, 0)");
            });
        }
    }
}