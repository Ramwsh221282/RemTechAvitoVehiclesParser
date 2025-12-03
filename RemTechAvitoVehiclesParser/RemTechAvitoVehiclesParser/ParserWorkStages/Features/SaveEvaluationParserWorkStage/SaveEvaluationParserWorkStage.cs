using RemTechAvitoVehiclesParser.ParserWorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Features.SaveEvaluationParserWorkStage;

public sealed class SaveEvaluationParserWorkStage(
    NpgSqlParserWorkStagesStorage stagesStorage,
    NpgSqlPaginationEvaluationParsersStorage parsersStorage
) : ISaveEvaluationParserWorkStage
{
    public async Task<(ParserWorkStageSnapshot stage, PaginationEvaluationParserSnapshot parser)> Handle(
        SaveEvaluationParserWorkStageCommand command, 
        CancellationToken ct = default)
    {
        ParserWorkStage stage = new ParserWorkStage.EvaluationWorkStage(command.Id);
        IEnumerable<PaginationEvaluationParserLink> links = command.Links.Select(
            l => new PaginationEvaluationParserLink(
                id: l.Id, 
                url: l.Url, 
                wasProcessed: false, 
                currentPage: null, 
                maxPage: null)
            );
        PaginationEvaluationParser parser = new(command.Id, command.Domain, command.Type, links);
        await stagesStorage.Save(stage, ct);
        await parsersStorage.Save(parser, ct, withLinks: true);
        ParserWorkStageSnapshot stageSnapshot = stage.GetSnapshot();
        PaginationEvaluationParserSnapshot parserSnapshot = parser.GetSnapshot();
        return (stageSnapshot, parserSnapshot);
    }
}