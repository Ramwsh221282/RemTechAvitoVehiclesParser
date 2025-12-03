using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public sealed record ParserWorkStageSnapshot(
    Guid Id, 
    string Name, 
    DateTime Created, 
    DateTime? Finished) 
    : ISnapshot;