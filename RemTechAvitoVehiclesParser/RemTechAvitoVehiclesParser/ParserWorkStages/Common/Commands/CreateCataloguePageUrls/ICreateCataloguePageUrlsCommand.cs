using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common.Commands.CreateCataloguePageUrls;

public interface ICreateCataloguePageUrlsCommand
{
    Task<CataloguePageUrl[]> Handle();
}