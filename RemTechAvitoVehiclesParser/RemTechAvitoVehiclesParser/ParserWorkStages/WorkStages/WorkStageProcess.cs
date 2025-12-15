namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages;

public delegate Task WorkStageProcess(WorkStageProcessDependencies deps, CancellationToken ct = default);

