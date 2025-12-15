namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

public static class WorkStageConstants
{
    public const string EvaluationStageName = "EVALUATION";
    public const string CatalogueStageName = "CATALOGUE";
    public const string ConcreteItemStageName = "CONCRETE";
    public const string FinalizationStage = "FINALIZATION";
    public const string SleepingStage = "SLEEPING";
}

public record ParserWorkStage(Guid Id, string Name)
{
    public ParserWorkStage(ParserWorkStage origin, string name) : 
    this(origin.Id, name) { }
}
public sealed record EvaluationWorkStage(ParserWorkStage Stage) : ParserWorkStage(Stage, WorkStageConstants.EvaluationStageName);
public sealed record CatalogueWorkStage(ParserWorkStage Stage) : ParserWorkStage(Stage, WorkStageConstants.CatalogueStageName);
public sealed record ConcreteItemWorkStage(ParserWorkStage Stage) : ParserWorkStage(Stage, WorkStageConstants.ConcreteItemStageName);
public sealed record FinalizationWorkStage(ParserWorkStage Stage) : ParserWorkStage(Stage, WorkStageConstants.FinalizationStage);
public sealed record SleepingWorkStage(ParserWorkStage Stage) : ParserWorkStage(Stage, WorkStageConstants.SleepingStage);

public static class ParserWorkStageImplementation
{
    extension(ParserWorkStage stage)
    {
    
        public ParserWorkStage ChangeStage<T>(T other) where T : ParserWorkStage
        {
            return stage with { Name = other.Name };
        }
    }
}

public static class ParserWorkStageConstruction
{
    extension(ParserWorkStage)
    {
        public static ParserWorkStage New(string name) => new
        (
            Id: Guid.NewGuid(),
            Name: name            
        );    
    }
}