using RemTechAvitoVehiclesParser.Parsing.BackgroundTasks;

namespace RemTechAvitoVehiclesParser.Parsing;

public static class AvitoParsingDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterAvitoParsing()
        {
            services.AddBypassFactory();
            services.AddParserWorkStartEventListener();
        }
        
        private void AddBypassFactory()
        {
            services.AddSingleton<AvitoBypassFactory>();
        }
        
        private void AddParserWorkStartEventListener()
        {
            services.AddHostedService<ParserWorkStartListenerService>();
        }
    }
}