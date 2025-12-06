using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage.Decorators;

public sealed class SaveEvaluationParserWorkStageLogging(
    Serilog.ILogger logger,
    ISaveEvaluationParserWorkStage origin
) :
    ISaveEvaluationParserWorkStage
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ISaveEvaluationParserWorkStage>();
    
    public async Task<(ParserWorkStageSnapshot stage, PaginationEvaluationParserSnapshot parser)> Handle(SaveEvaluationParserWorkStageCommand command, CancellationToken ct = default)
    {
        _logger.Information("Saving evaluation parser work stage...");
        (ParserWorkStageSnapshot stage, PaginationEvaluationParserSnapshot parser) result = await origin.Handle(command, ct);
        ParserWorkStageSnapshot stage = result.stage;
        PaginationEvaluationParserSnapshot parser = result.parser;
        
        _logger.Information("""
                            Saved evaluation parser work stage:
                            Id: {Id}
                            Stage: {Stage}
                            Created: {Created}
                            """, stage.Id, stage.Name, stage.Created);
        
        _logger.Information(
            """
            Saved parser for page evaluation:
            Id: {Id}
            Type: {Type}
            Domain: {Domain}
            Links Count: {LinksCount}
            """, parser.Id, parser.Type, parser.Domain, parser.Links.Count
            );
        
        return result;
    }
}