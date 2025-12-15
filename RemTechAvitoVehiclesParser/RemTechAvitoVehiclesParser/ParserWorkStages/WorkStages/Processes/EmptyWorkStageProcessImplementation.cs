namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

public static class EmptyWorkStageProcessImplementation
{
    extension(WorkStageProcess)
    {
        public static WorkStageProcess Empty => (page, deps) => Task.CompletedTask;
    }
}
