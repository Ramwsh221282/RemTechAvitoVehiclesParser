using PuppeteerSharp;

namespace ParsingSDK.Parsing;

public static class PageActions
{
    extension(IPage page)
    {
        public async Task<Maybe<IElementHandle>> GetElementRetriable(string selectorQuery, int retryAmount = 30)
        {
            int currentAttempt = 0;
            while (currentAttempt < retryAmount)
            {
                Maybe<IElementHandle> maybe = await Maybe<IElementHandle>.Resolve(() => page.QuerySelectorAsync(selectorQuery));
                if (maybe.HasValue) return maybe;
                await Task.Delay(TimeSpan.FromSeconds(1));
                currentAttempt++;
            }
                
            return Maybe<IElementHandle>.None();
        }
        
        public async Task ScrollBottom()
        {
            await page.EvaluateExpressionAsync("window.scrollBy(0, document.documentElement.scrollHeight)");
        }

        public async Task ScrollTop()
        {
            await page.EvaluateExpressionAsync("window.scrollTo(0, 0)");
        }
        
        public async Task NavigatePage(string url)
        {
            WaitUntilNavigation waitUntil = WaitUntilNavigation.DOMContentLoaded;
            try
            {
                await page.GoToAsync(url, timeout: 0, [waitUntil]);
            }
            catch(NavigationException)
            {
                Console.WriteLine($"Puppeteer timeout navigation exceeded.");
            }
        }
    }
}