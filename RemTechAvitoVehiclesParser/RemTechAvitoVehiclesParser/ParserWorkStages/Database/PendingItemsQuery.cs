namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed record PendingItemsQuery(bool UnprocessedOnly = false, bool WithLock = false, int? Limit = null);