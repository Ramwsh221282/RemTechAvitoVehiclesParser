namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.HoverCatalogueItemImages;

public sealed class HoverCatalogueItemImagesLogging(Serilog.ILogger logger, IHoverCatalogueItemImagesCommand inner) : IHoverCatalogueItemImagesCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IHoverCatalogueItemImagesCommand>();
    
    public async Task Handle()
    {
        Logger.Information("Hovering catalogue item images");
        
        try
        {
            await inner.Handle();
            Logger.Information("Hovered catalogue item images");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error at hovering catalogue item images");
            throw;
        }
    }
}