namespace RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Database;

public sealed class NpgSqlPendingItemRow
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Address { get; init; }
    public required long Price { get; init; }
    public required bool IsNds { get; init; }
    public required string DescriptionList { get; init; }
    public required string Characteristics { get; init; }
    public required bool WasProcessed { get; init; }
    public required string Photos { get; init; }
}