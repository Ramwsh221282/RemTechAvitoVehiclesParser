using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.CreateCataloguePageUrls;

public sealed class CreateCataloguePageUrlsLogging(Serilog.ILogger logger, ICreateCataloguePageUrlsCommand inner) : ICreateCataloguePageUrlsCommand
{
    public Serilog.ILogger Logger { get; } = logger.ForContext<ICreateCataloguePageUrlsCommand>();
    
    public async Task<CataloguePageUrl[]> Handle()
    {
        Logger.Information("Extracting catalogue page urls");
        
        try
        {
            CataloguePageUrl[] urls = await inner.Handle();
            
            Logger.Information("Extracted {Length} catalogue page urls", urls.Length);
            foreach (CataloguePageUrl url in urls)
                Logger.Information("Catalogue page url: {Url}", url.Url);
            
            return urls;
 
        }
        catch(Exception ex)
        {
            Logger.Error(ex, "Error at extracting catalogue page urls");
            throw;
        }
    }
}