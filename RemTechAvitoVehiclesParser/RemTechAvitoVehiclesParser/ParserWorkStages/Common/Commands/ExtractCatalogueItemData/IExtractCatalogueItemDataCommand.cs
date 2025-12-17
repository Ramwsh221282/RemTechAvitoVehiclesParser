namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractCatalogueItemData;

public interface IExtractCatalogueItemDataCommand
{
    Task<AvitoVehicle[]> Handle();
}