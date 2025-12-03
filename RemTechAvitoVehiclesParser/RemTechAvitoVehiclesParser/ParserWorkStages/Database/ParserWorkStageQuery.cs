namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed record ParserWorkStageQuery(
    Guid? Id = null, 
    string? Name = null, 
    bool OnlyFinished = false, 
    bool OnlyNotFinished = false,
    bool WithLock = false,
    int? Limit = null);