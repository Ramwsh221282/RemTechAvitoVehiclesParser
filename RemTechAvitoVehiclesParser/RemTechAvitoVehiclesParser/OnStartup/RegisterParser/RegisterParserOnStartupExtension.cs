namespace RemTechAvitoVehiclesParser.OnStartup.RegisterParser;

public static class RegisterParserOnStartupExtension
{
    extension(WebApplication app)
    {
        public async Task RegisterParser(string domain, string type)
        {
            IServiceProvider provider = app.Services;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IRegisterParserOnStartup service = scope.ServiceProvider.GetRequiredService<IRegisterParserOnStartup>();
            await service.Invoke(domain, type);
        }
    }
}