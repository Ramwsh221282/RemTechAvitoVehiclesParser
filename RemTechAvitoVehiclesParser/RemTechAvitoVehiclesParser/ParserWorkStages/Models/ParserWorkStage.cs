using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Models;

public class ParserWorkStage(Guid id, string name, DateTime created, DateTime? finished) : ISnapshotSource<ParserWorkStage, ParserWorkStageSnapshot>
{
    private readonly Guid _id = id;
    private readonly string _name = name;
    private readonly DateTime _created = created;
    private readonly DateTime? _finished = finished;

    public ParserWorkStage Finish(DateTime finished)
    {
        if (_finished.HasValue)
            throw new InvalidOperationException(
                """
                Cannot finish work stage. Work stage is already finished.
                If you want finish work stage you have to created a new one.
                """
            );
        return new ParserWorkStage(this, finished: finished);
    }

    public ParserWorkStage ChangeStage<T>(T stage) where T : ParserWorkStage
    {
        if (_finished.HasValue)
            throw new InvalidOperationException(
                """
                Cannot change work stage. Work stage is already finished.
                Only not finished (under work) stages are allowed to be changed.
                """
            );
        return new ParserWorkStage(this, name: stage._name);
    }
    
    private ParserWorkStage(
        ParserWorkStage origin,
        string? name = null,
        DateTime? created = null,
        DateTime? finished = null)
    : this(
        origin._id,
        name ?? origin._name,
        created ?? origin._created,
        finished ?? origin._finished
        ) { }

    public ParserWorkStageSnapshot GetSnapshot()
    {
        return new ParserWorkStageSnapshot(_id, _name, _created, _finished);
    }

    public sealed class EvaluationWorkStage(ParserWorkStage stage) 
        : ParserWorkStage(stage, name: WorkStageConstants.EvaluationStageName)
    {
        public EvaluationWorkStage(Guid id) 
            : this(new ParserWorkStage(id, "EVALUATION", DateTime.UtcNow, null)) { }
    }
    
    public sealed class CatalogueWorkStage(ParserWorkStage stage) 
        : ParserWorkStage(stage, name: WorkStageConstants.CatalogueStageName);
    
    public sealed class ConcreteItemWorkStage(ParserWorkStage stage) 
        : ParserWorkStage(stage, name: WorkStageConstants.ConcreteItemStageName);
}

public static class WorkStageConstants
{
    public const string EvaluationStageName = "EVALUATION";
    public const string CatalogueStageName = "CATALOGUE";
    public const string ConcreteItemStageName = "CONCRETE";
}