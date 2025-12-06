using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public static class PaginationParsingParserLinkImplementation
{
    extension(PaginationParsingParserLink origin)
    {
        public PaginationParsingParserLink MarkProcessed()
        {
            if (origin.WasProcessed)
                throw new InvalidOperationException(
                    """
                    Cannot mark link as processed.
                    Link is already processed.
                    """
                );
            return origin with { WasProcessed = true };
        }

        public CataloguePageUrl[] BuildCataloguePageUrls()
        {
            if (!origin.CurrentPage.HasValue)
                throw new InvalidOperationException(
                    """
                    Cannot build catalogue page urls from parser link.
                    Parser link has no current page initialized.
                    """
                );
        
            if (!origin.MaxPage.HasValue)
                throw new InvalidOperationException(
                    """
                    Cannot build catalogue page urls from parser link.
                    Parser link has no max page initialized.
                    """
                );
        
            int pageCounter = origin.CurrentPage.Value;
            List<CataloguePageUrl> urls = [];
            while (pageCounter <= origin.MaxPage.Value)
            {
                Guid id = Guid.NewGuid();
                string urlValue = $"{origin.Url}?page={pageCounter}";
                urls.Add(new CataloguePageUrl(Id: id, LinkId: origin.Id, Url: urlValue, Processed: false, RetryCount: 0, []));
                pageCounter++;
            }
        
            return urls.ToArray();
        }
        
        public PaginationParsingParserLink IncrementCurrentPage()
        {
            if (!origin.CurrentPage.HasValue)
                throw new InvalidOperationException(
                    """
                    Cannot increment current page.
                    Current page and max page are not initialized.
                    """
                );
            int nextCurrentPage = origin.CurrentPage.Value + 1;
            return origin with { CurrentPage = nextCurrentPage };
        }
    
        public PaginationParsingParserLink AddPagination(int currentPage, int maxPage)
        {
            if (origin.CurrentPage.HasValue && origin.MaxPage.HasValue)
                throw new InvalidOperationException("Cannot add pagination for parser link as it is already set.");
            return origin with { CurrentPage = currentPage, MaxPage = maxPage };
        }
    }
}