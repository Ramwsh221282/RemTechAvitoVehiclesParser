namespace RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Database;

public sealed record PendingItemsQuery(bool UnprocessedOnly = false, bool WithLock = false, int? Limit = null);