using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;

public static class CataloguePageUrlImplementation
{
    extension(CataloguePageUrl url)
    {
        public CataloguePageUrl MarkProcessed()
        {
            if (url.Processed)
                throw new InvalidOperationException(
                    """
                    Cannot mark catalogue page url as processed.
                    Catalogue page url is already processed.
                    """
                );
            return url with { Processed = true };
        }

        public CataloguePageUrl IncrementRetryCount()
        {
            int nextRetryCount = url.RetryCount + 1;
            return url with { RetryCount = nextRetryCount };
        }

        public CataloguePageUrl AddItems(IEnumerable<CataloguePageItem> items)
        {
            return url with { Items = [.. items] };
        }
    }
}
