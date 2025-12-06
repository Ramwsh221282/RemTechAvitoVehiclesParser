using PuppeteerSharp;

namespace ParsingSDK.Parsing;

public static class ElementHandleActions
{
    extension(IElementHandle element)
    {
        public async Task<Maybe<string>> GetElementInnerText()
        {
            string? text = await element.EvaluateFunctionAsync<string?>("el => el.innerText");
            return Maybe<string>.Resolve(text);
        }

        public async Task<Maybe<string>> GetAttribute(string attributeName)
        {
            string? attribute = await element.EvaluateFunctionAsync<string?>($"el => el.getAttribute('{attributeName}')");
            return Maybe<string>.Resolve(attribute);
        }
        
        public async Task<IElementHandle[]> GetElements(string selectorQuery)
        {
            IElementHandle[] elements = await element.QuerySelectorAllAsync(selectorQuery);
            return elements;
        }

        public async Task<Maybe<IElementHandle>> GetElementRetriable(string selectorQuery, int retryAmount = 30)
        {
            int currentAttempt = 0;
            while (currentAttempt < retryAmount)
            {
                Maybe<IElementHandle> maybe = await Maybe<IElementHandle>.Resolve(async () => await element.QuerySelectorAsync(selectorQuery));
                if (maybe.HasValue) return maybe;
                currentAttempt++;
            }
            
            return Maybe<IElementHandle>.None();
        }
    }
}