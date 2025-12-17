namespace RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing;

public record ProcessingParserLink(Guid Id, string Url, bool WasProcessed, int RetryCount)
{
    public ProcessingParserLink MarkProcessed() => this with { WasProcessed = true };
    public ProcessingParserLink IncreaseRetryCount() => this with { RetryCount = RetryCount + 1 };
}
