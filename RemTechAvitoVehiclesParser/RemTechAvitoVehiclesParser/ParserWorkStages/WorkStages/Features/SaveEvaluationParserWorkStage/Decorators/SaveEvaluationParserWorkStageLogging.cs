using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage.Decorators;

public sealed class SaveEvaluationParserWorkStageLogging(
    Serilog.ILogger logger,
    ISaveEvaluationParserWorkStage origin
) : ISaveEvaluationParserWorkStage
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ISaveEvaluationParserWorkStage>();

    public async Task<(ParserWorkStage stage, ProcessingParser parser)> Handle(
        SaveEvaluationParserWorkStageCommand command,
        CancellationToken ct = default
    )
    {
        _logger.Information("Saving evaluation parser work stage...");
        (ParserWorkStage stage, ProcessingParser parser) result = await origin.Handle(command, ct);
        ParserWorkStage stage = result.stage;
        ProcessingParser parser = result.parser;

        _logger.Information(
            """
            Saved evaluation parser work stage:
            Id: {Id}
            Stage: {Stage}                            
            """,
            stage.Id,
            stage.Name
        );

        _logger.Information(
            """
            Saved parser for page evaluation:
            Id: {Id}
            Type: {Type}
            Domain: {Domain}
            Links Count: {LinksCount}
            """,
            parser.Id,
            parser.Type,
            parser.Domain,
            parser.Links.Count
        );

        return result;
    }
}
