namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

public static class WorkStageConstants
{
    public const string EvaluationStageName = "EVALUATION";
    public const string CatalogueStageName = "CATALOGUE";
    public const string ConcreteItemStageName = "CONCRETE";
    public const string FinalizationStage = "FINALIZATION";
    public const string SleepingStage = "SLEEPING";
}

public class ParserWorkStage
{
    public Guid Id { get; private init; }
    public string Name { get; private set; }

    public static ParserWorkStage PaginationStage() =>
        new() { Id = Guid.NewGuid(), Name = WorkStageConstants.EvaluationStageName };

    public static ParserWorkStage Create(Guid id, string name) => new() { Id = id, Name = name };

    public void ToCatalogueStage() => Name = WorkStageConstants.CatalogueStageName;

    public void ToConcreteStage() => Name = WorkStageConstants.ConcreteItemStageName;

    public void ToFinalizationStage() => Name = WorkStageConstants.FinalizationStage;

    public void ToSleepingStage() => Name = WorkStageConstants.SleepingStage;

    private ParserWorkStage()
    {
        Id = Guid.NewGuid();
        Name = string.Empty;
    }
}
