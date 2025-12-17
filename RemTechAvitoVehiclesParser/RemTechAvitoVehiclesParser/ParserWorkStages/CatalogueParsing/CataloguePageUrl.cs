namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing;

public sealed record CataloguePageUrl(string Url, bool Processed, int RetryCount)
{
    public CataloguePageUrl MarkProcessed() => this with { Processed = true };
    public CataloguePageUrl IncrementRetryCount() => this with { RetryCount = RetryCount + 1 };
}
