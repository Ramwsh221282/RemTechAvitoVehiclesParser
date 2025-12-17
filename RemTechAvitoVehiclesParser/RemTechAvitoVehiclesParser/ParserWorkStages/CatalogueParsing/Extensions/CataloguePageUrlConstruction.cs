namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Extensions;

public static class CataloguePageUrlConstruction
{
    extension(CataloguePageUrl)
    {
        public static CataloguePageUrl New(string url)
        {
            return new CataloguePageUrl(url, Processed: false, RetryCount: 0);
        }
    }
}
