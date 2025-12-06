namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;

public sealed record WorkStageQuery(
    Guid? Id = null, 
    string? Name = null, 
    bool OnlyFinished = false, 
    bool OnlyNotFinished = false,
    bool WithLock = false,
    int? Limit = null);