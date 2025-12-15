using RemTechAvitoVehiclesParser.Parsing.BackgroundTasks;

namespace RemTechAvitoVehiclesParser.Parsing;

public static class AvitoParsingDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterAvitoParsing()
        {
            services.AddParserWorkStartEventListener();
        }

        private void AddParserWorkStartEventListener()
        {
            services.AddHostedService<ParserWorkStartListenerService>();
        }
    }
}