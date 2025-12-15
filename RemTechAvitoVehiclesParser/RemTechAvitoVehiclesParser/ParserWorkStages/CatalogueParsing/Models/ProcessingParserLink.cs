namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

public record ProcessingParserLink(Guid Id, string Url, bool WasProcessed, int RetryCount);
