namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.HoverCatalogueItemImages;

public static class HoverCatalogueItemImagesCommandDecorating
{
    public static IHoverCatalogueItemImagesCommand UseLogging(this IHoverCatalogueItemImagesCommand command, Serilog.ILogger logger)
        => new HoverCatalogueItemImagesLogging(logger, command);
}