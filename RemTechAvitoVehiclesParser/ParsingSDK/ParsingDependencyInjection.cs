using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;

namespace ParsingSDK;

public static class ParsingDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserDependencies()
        {
            services.AddBrowserFactory();
        }
        
        public void AddBrowserFactory()
        {
            services.AddSingleton<BrowserFactory>();
        }
    }
}