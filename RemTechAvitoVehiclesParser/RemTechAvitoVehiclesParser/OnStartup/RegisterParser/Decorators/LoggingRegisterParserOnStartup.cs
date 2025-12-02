namespace RemTechAvitoVehiclesParser.OnStartup.RegisterParser.Decorators;

public sealed class LoggingRegisterParserOnStartup(
    Serilog.ILogger logger,
    IRegisterParserOnStartup origin
    )
    : IRegisterParserOnStartup
{
    private readonly Serilog.ILogger _logger = logger;
    private readonly IRegisterParserOnStartup _origin = origin;

    public async Task Invoke(string domain, string type)
    {
        object[] logProperties = [domain, type];
        try
        {
            _logger.Information("Creating a ticket to register parser: {Domain} {Type}", logProperties);
            await _origin.Invoke(domain, type);
            _logger.Information("Created ticket to register parser: {Domain} {Type}", logProperties);
        }
        catch(Exception ex)
        {
            _logger.Error("Error at registering parser: {Error}", ex.Message);
            throw;
        }
    }
}