using RemTechAvitoVehiclesParser.Utilities.TextTransforming;

namespace RemTechAvitoVehiclesParser.Utilities;

public static class UtilitiesDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterUtilities()
        {
            services.AddTextTransformerBuilder();
        }

        private void AddTextTransformerBuilder()
        {
            services.AddSingleton<TextTransformerBuilder>();
        }
    }
}