namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public sealed record ProcessingParserLinkQuery(
    bool ProcessedOnly = false,
    bool UnprocessedOnly = false,
    int? RetryLimit = null,
    bool WithLock = false
);
