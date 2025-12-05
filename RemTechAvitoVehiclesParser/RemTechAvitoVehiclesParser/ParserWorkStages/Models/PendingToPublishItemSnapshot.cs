using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed class PendingToPublishItemSnapshot : ISnapshot
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Address { get; init; }
    public required long Price { get; init; }
    public required bool IsNds { get; init; }
    public required IReadOnlyList<string> DescriptionList { get; init; }
    public required IReadOnlyList<string> Characteristics { get; init; }
    public required IReadOnlyList<string> Photos { get; init; }
    public required bool WasProcessed { get; init; }
}