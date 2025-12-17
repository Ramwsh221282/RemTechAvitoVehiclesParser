namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractConcreteItem;

public sealed class ExtractConcreteItemLogging(Serilog.ILogger logger, IExtractConcreteItemCommand inner)
    : IExtractConcreteItemCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IExtractConcreteItemCommand>();
    
    public async Task<AvitoVehicle> Handle()
    {
        Logger.Information("Extracting concrete item data");
        
        try
        {
            AvitoVehicle vehicle = await inner.Handle();
            AvitoVehicleConcretePageRepresentation concrete = vehicle.ConcretePageRepresentation;
            
            Logger.Information("""
                               Extracted concrete item data. Info: 
                               Title: {Title}
                               """, concrete.Title);
            
            foreach (KeyValuePair<string, string> ctx in concrete.Characteristics)
                Logger.Information("Characteristic: {Name} - {Value}", ctx.Key, ctx.Value);
            
            return vehicle;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error at extracting concrete item data");
            throw;
        }
    }
}