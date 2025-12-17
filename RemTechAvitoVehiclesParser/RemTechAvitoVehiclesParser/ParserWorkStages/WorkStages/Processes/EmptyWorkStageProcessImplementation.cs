namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class EmptyWorkStageProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess Empty => (deps, ct) =>
        {
            Serilog.ILogger logger = deps.Logger.ForContext<WorkStageProcess>();
            logger.Information("Empty work stage. Sleeping.");
            return Task.CompletedTask;
        };
    }
}
