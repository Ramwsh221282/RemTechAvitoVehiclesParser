using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage;

public sealed class SaveEvaluationParserWorkStage(
    NpgSqlSession session,
    NpgSqlPaginationParsingParsersStorage parsersStorage
) : ISaveEvaluationParserWorkStage
{
    public async Task<(ParserWorkStage stage, ProcessingParser parser)> Handle(
        SaveEvaluationParserWorkStageCommand command,
        CancellationToken ct = default
    )
    {
        ParserWorkStage stage = new EvaluationWorkStage(
            new ParserWorkStage(command.Id, Name: WorkStageConstants.EvaluationStageName)
        );
        ProcessingParser parser = new(command.Id, command.Domain, command.Type, []);
        IEnumerable<ProcessingParserLink> links = command.Links.Select(l =>
            ProcessingParserLink.FromParser(parser, l.Id, l.Url)
        );

        ProcessingParser withLinks = parser.AddLinks(links);
        await stage.Persist(session, ct);
        await parsersStorage.Save(withLinks, ct, withLinks: true);
        return (stage, withLinks);
    }
}
