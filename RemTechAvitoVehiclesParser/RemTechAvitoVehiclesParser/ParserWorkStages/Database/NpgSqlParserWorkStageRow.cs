using RemTechAvitoVehiclesParser.ParserWorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed class NpgSqlParserWorkStageRow
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime Created { get; init; }
    public required DateTime? Finished { get; init; }

    public ParserWorkStage ToWorkStage()
    {
        return new ParserWorkStage(Id, Name, Created, Finished);
    }
}