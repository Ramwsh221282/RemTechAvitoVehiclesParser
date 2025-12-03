using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage.Decorators;

public sealed class SaveEvaluationParserWorkStageLogging(
    Serilog.ILogger logger,
    ISaveEvaluationParserWorkStage origin
) :
    ISaveEvaluationParserWorkStage
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ISaveEvaluationParserWorkStage>();
    
    public async Task<ParserWorkStageSnapshot> Handle(SaveEvaluationParserWorkStageCommand command, CancellationToken ct = default)
    {
        _logger.Information("Saving evaluation parser work stage...");
        ParserWorkStageSnapshot result = await origin.Handle(command, ct);
        _logger.Information("""
                            Saved evaluation parser work stage:
                            Id: {Id}
                            Stage: {Stage}
                            Created: {Created}
                            """, result.Id, result.Name, result.Created);
        return result;
    }
}