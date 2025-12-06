using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed class NpgSqlCataloguePageUrlRow
{
    public required Guid Id { get; init; }
    public required Guid LinkId { get; init; }
    public required string Url { get; init; }
    public required bool WasProcessed { get; init; }
    public required int RetryCount { get; init; }

    public CataloguePageUrl ToCataloguePageUrl()
    {
        return new CataloguePageUrl(Id, LinkId, Url, WasProcessed, RetryCount);
    }
}