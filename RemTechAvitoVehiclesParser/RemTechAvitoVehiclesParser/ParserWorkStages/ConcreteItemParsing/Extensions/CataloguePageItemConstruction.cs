namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;

public static class CataloguePageItemConstruction
{
    extension(CataloguePageItem)
    {
        public static CataloguePageItem New(string id, string url, string payload) =>
            new(id, url, payload, WasProcessed: false, RetryCount: 0);
    }
}
