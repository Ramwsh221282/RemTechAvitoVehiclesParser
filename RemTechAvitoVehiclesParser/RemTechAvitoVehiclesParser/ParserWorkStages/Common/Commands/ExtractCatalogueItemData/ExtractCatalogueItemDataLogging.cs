namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.ExtractCatalogueItemData;

public sealed class ExtractCatalogueItemDataLogging(Serilog.ILogger logger, IExtractCatalogueItemDataCommand inner) : IExtractCatalogueItemDataCommand
{
    public Serilog.ILogger Logger { get; } = logger.ForContext<IExtractCatalogueItemDataCommand>();
    
    public async Task<AvitoVehicle[]> Handle()
    {
         Logger.Information("Extracting catalogue item data");
        
        try
        {
            AvitoVehicle[] data = await inner.Handle();
             Logger.Information("Extracted {Length} catalogue item data", data.Length);
            
            foreach (AvitoVehicle vehicle in data)
            {
                AvitoVehicleCatalogueRepresentation catalogue = vehicle.CatalogueRepresentation;
                Logger.Information("""
                                   Catalogue item data info: 
                                   Url: {Url}
                                   Id: {Id}
                                   Address: {Address}
                                   Price: {Price}
                                   IsNds: {IsNds}
                                   Photos count: {Photos}
                                   """, 
                    catalogue.Url,
                    catalogue.Id,
                    catalogue.Address,
                    catalogue.Price,
                    catalogue.IsNds,
                    catalogue.Photos.Length);
            }
            
            return data;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error at extracting catalogue item data");
            throw;
        }
    }
}