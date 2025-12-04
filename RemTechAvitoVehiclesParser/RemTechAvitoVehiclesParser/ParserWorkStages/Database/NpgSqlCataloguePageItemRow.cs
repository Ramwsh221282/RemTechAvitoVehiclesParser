using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed class NpgSqlCataloguePageItemRow
{
    public required string Id { get; init; }
    public required Guid CatalogueUrlId { get; init; }
    public required bool WasProcessed { get; init; }
    public required int RetryCount { get; init; }
    public required string Payload { get; init; }

    public CataloguePageItem ToCataloguePageItem()
    {
        return new CataloguePageItem(
            id: Id, 
            catalogueUrlId: CatalogueUrlId, 
            payload: Payload, 
            wasProcessed: WasProcessed, 
            retryCount: RetryCount);
    }
}