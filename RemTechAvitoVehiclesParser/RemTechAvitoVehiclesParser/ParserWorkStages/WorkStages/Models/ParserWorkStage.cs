namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

public static class WorkStageConstants
{
    public const string EvaluationStageName = "EVALUATION";
    public const string CatalogueStageName = "CATALOGUE";
    public const string ConcreteItemStageName = "CONCRETE";
    public const string FinalizationStage = "FINALIZATION";
    public const string SleepingStage = "SLEEPING";
}

public record ParserWorkStage(Guid Id, string Name, DateTime Created, DateTime? Finished)
{
    public ParserWorkStage(ParserWorkStage origin, string name) : this(origin.Id, name, origin.Created, origin.Finished) { }
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
        public ParserWorkStage Finish(DateTime finishDate)
        {
            if (stage.Finished.HasValue)
                throw new InvalidOperationException(
                    """
                    Cannot finish work stage. Work stage is already finished.
                    If you want finish work stage you have to created a new one.
                    """
                );
            return stage with { Finished = finishDate };
        }
        
        public ParserWorkStage ChangeStage<T>(T other) where T : ParserWorkStage
        {
            if (stage.Finished.HasValue)
                throw new InvalidOperationException(
                    """
                    Cannot change work stage. Work stage is already finished.
                    Only not finished (under work) stages are allowed to be changed.
                    """
                );
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
            Name: name,
            Created: DateTime.UtcNow,
            Finished: null
        );
        
        public static ParserWorkStage MapFrom<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, string> nameMap,
            Func<T, DateTime> createdMap,
            Func<T, DateTime?> finishMap
        ) 
            where T : class =>
            new
            (
                Id: idMap(source),
                Name: nameMap(source),
                Created: createdMap(source),
                Finished: finishMap(source)
            );
    }
}