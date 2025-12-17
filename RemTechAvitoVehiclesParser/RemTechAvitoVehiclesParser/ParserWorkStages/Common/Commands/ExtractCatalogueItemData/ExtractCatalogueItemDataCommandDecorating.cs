namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractCatalogueItemData;

public static class ExtractCatalogueItemDataCommandDecorating
{
    extension(IExtractCatalogueItemDataCommand command)
    {
        public IExtractCatalogueItemDataCommand UseLogging(Serilog.ILogger logger)
            => new ExtractCatalogueItemDataLogging(logger, command);
    }
}