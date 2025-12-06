namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;

public sealed class NpgSqlParserWorkStageRow
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime Created { get; init; }
    public required DateTime? Finished { get; init; }
}