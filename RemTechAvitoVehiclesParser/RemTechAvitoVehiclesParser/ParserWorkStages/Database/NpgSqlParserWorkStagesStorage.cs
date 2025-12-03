using System.Data;
using Dapper;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

public sealed class NpgSqlParserWorkStagesStorage(NpgSqlSession session)
{
    public async Task Save<T>(ISnapshotSource<T, ParserWorkStageSnapshot> snapshotSource, CancellationToken ct = default)
        where T : class
    {
        const string sql = """
                           INSERT INTO avito_parser_module.work_stages
                           (id, name, created, finished)
                           VALUES
                           (@id, @name, @created, @finished);
                           """;
        ParserWorkStageSnapshot snapshot = snapshotSource.GetSnapshot();
        CommandDefinition command = session.CreateCommand(sql, snapshot, CreateParameters, ct);
        await session.ExecuteCommand(command);
    }

    public async Task<Maybe<ParserWorkStage>> GetWorkStage(ParserWorkStageQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = FilterClause(query);
        string lockClause = LockClause(query);
        string sql = $"""
                      SELECT id, name, created, finished
                      FROM avito_parser_module.work_stages
                      {filterSql}
                      {lockClause}
                      LIMIT 1
                      """;
        CommandDefinition command = session.CreateCommand(sql, parameters, ct);
        Maybe<NpgSqlParserWorkStageRow> row = await session.QuerySingle<NpgSqlParserWorkStageRow>(command, ct);
        return row.HasValue
            ? Maybe<ParserWorkStage>.Some(row.Value.ToWorkStage())
            : Maybe<ParserWorkStage>.None();
    }
    
    public async Task<IEnumerable<ParserWorkStage>> GetWorkStages(
        ParserWorkStageQuery query,
        CancellationToken ct = default
        )
    {
        (DynamicParameters parameters, string filterSql) = FilterClause(query);
        string lockClause = LockClause(query);
        string limitClause = LimitClause(query);
        string sql = $"""
                      SELECT id, name, created, finished
                      FROM avito_parser_module.work_stages
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = session.CreateCommand(sql, parameters, ct);
        IEnumerable<NpgSqlParserWorkStageRow> rows = await session.QueryMany<NpgSqlParserWorkStageRow>(command, ct);
        return rows.Select(r => r.ToWorkStage());
    }

    private static (DynamicParameters parameters, string filterSql) FilterClause(ParserWorkStageQuery query)
    {
        List<string> filters = [];
        DynamicParameters parameters = new();

        if (query.Id.HasValue)
        {
            filters.Add("id=@id");
            parameters.Add("@id", query.Id.Value, DbType.Guid);
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            filters.Add("name=@name");
            parameters.Add("@name", query.Name, DbType.String);
        }

        if (query.OnlyFinished)
        {
            filters.Add("finished is not null");
        }

        if (query.OnlyNotFinished)
        {
            filters.Add("finished is not null");
        }

        string resultSql = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        return (parameters, resultSql);
    }
        
    private static string LockClause(ParserWorkStageQuery query)
    {
        return query.WithLock ? "FOR UPDATE" : string.Empty;
    }

    private static string LimitClause(ParserWorkStageQuery query)
    {
        return query.Limit.HasValue ? $"LIMIT {query.Limit}" : string.Empty;
    }
    
    private static object CreateParameters(ParserWorkStageSnapshot snapshot)
    {
        return new
        {
            id = snapshot.Id,
            name = snapshot.Name,
            created = snapshot.Created,
            finished = snapshot.Finished,
        };
    }
}