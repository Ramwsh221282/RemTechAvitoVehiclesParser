using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage;

public sealed class SaveEvaluationParserWorkStage(
    NpgSqlSession session,
    NpgSqlPaginationParsingParsersStorage parsersStorage
) : ISaveEvaluationParserWorkStage
{
    public async Task<(ParserWorkStage stage, PaginationParsingParser parser)> Handle(
        SaveEvaluationParserWorkStageCommand command,
        CancellationToken ct = default)
    {
        ParserWorkStage stage = new EvaluationWorkStage(new ParserWorkStage(command.Id, Name: WorkStageConstants.EvaluationStageName));
        PaginationParsingParser parser = new(command.Id, command.Domain, command.Type, []);
        IEnumerable<PaginationParsingParserLink> links = command.Links.Select(l =>
            PaginationParsingParserLink.FromParser(parser, l.Id, l.Url));

        PaginationParsingParser withLinks = parser.AddLinks(links);
        await stage.Persist(session, ct);
        await parsersStorage.Save(withLinks, ct, withLinks: true);
        return (stage, withLinks);
    }
}