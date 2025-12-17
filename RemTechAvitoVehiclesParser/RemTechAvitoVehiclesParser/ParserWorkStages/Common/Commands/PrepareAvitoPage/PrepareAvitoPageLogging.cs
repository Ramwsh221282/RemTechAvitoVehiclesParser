namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.PrepareAvitoPage;

public sealed class PrepareAvitoPageLogging(Serilog.ILogger logger, IPrepareAvitoPageCommand inner) : IPrepareAvitoPageCommand
{
    public Serilog.ILogger Logger { get; } = logger.ForContext<IPrepareAvitoPageCommand>();
    
    public async Task Handle()
    {
        Logger.Information("Preparing Avito page");
        
        try
        {
            await inner.Handle();
            Logger.Information("Avito page prepared");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error at preparing Avito page");
            throw;
        }
    }
}