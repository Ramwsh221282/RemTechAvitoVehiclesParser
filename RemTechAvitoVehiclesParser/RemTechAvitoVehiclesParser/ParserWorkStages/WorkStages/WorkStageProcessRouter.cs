using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Processes;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages;

public static class WorkStageProcessRouter
{
    public static WorkStageProcess Route(ParserWorkStage workStage)
    {
        string name = workStage.Name;
        return name switch
        {
            WorkStageConstants.EvaluationStageName => WorkStageProcess.PaginationExtraction,
            WorkStageConstants.CatalogueStageName => WorkStageProcess.CatalogueProcess,
            WorkStageConstants.ConcreteItemStageName => WorkStageProcess.ConcreteItems,
            WorkStageConstants.FinalizationStage => WorkStageProcess.Finalization,
            WorkStageConstants.SleepingStage => WorkStageProcess.Empty,
            _ => throw new InvalidOperationException($"Unsupported stage name: {name}")    
        };
    }
}
