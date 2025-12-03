using RemTechAvitoVehiclesParser.Parsing.BackgroundTasks;

namespace RemTechAvitoVehiclesParser.Parsing;

public static class ParsingDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParsingContext()
        {
            services.AddParserWorkStartEventListener();
            services.AddBrowserFactory();
        }

        public void AddBrowserFactory()
        {
            services.AddSingleton<BrowserFactory>();
        }
        
        public void AddParserWorkStartEventListener()
        {
            services.AddHostedService<ParserWorkStartListenerService>();
        }
    }
}