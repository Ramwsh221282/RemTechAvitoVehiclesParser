namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;

public sealed class NpgSqlCataloguePageItemRow
{
    public required string Id { get; init; }
    public required Guid CatalogueUrlId { get; init; }
    public required bool WasProcessed { get; init; }
    public required int RetryCount { get; init; }
    public required string Payload { get; init; }
}