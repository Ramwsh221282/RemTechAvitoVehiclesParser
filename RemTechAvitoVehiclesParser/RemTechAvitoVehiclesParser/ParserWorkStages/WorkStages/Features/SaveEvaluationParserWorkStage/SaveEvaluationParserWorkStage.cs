using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage;

public sealed class SaveEvaluationParserWorkStage(
    NpgSqlParserWorkStagesStorage stagesStorage,
    NpgSqlPaginationParsingParsersStorage parsersStorage
) : ISaveEvaluationParserWorkStage
{
    public async Task<(ParserWorkStage stage, PaginationParsingParser parser)> Handle(
        SaveEvaluationParserWorkStageCommand command, 
        CancellationToken ct = default)
    {
        ParserWorkStage stage = new EvaluationWorkStage(new ParserWorkStage(command.Id, Name: WorkStageConstants.EvaluationStageName, DateTime.UtcNow, null));
        PaginationParsingParser parser = new(command.Id, command.Domain, command.Type, []);
        IEnumerable<PaginationParsingParserLink> links = command.Links.Select(l => 
            PaginationParsingParserLink.FromParser(parser, l.Id, l.Url));

        PaginationParsingParser withLinks = parser.AddLinks(links);
        await stagesStorage.Save(stage, ct);
        await parsersStorage.Save(withLinks, ct, withLinks: true);
        return (stage, withLinks);
    }
}