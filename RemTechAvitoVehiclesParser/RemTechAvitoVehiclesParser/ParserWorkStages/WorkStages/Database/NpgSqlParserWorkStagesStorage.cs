using Dapper;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Database;

public sealed class NpgSqlParserWorkStagesStorage(NpgSqlSession session)
{
    public async Task Save(ParserWorkStage stage, CancellationToken ct = default)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.work_stages
                           (id, name, created, finished)
                           VALUES
                           (@id, @name, @created, @finished);
                           """;
        CommandDefinition command = session.FormCommand(sql, stage.ExtractParameters(), ct);
        await session.Execute(command);
    }

    public async Task Update(ParserWorkStage stage, CancellationToken ct = default)
    {
        const string sql = """
                           UPDATE avito_parser_module.work_stages
                           SET name = @name,
                               finished = @finished
                           WHERE id = @id;
                           """;
        CommandDefinition command = session.FormCommand(sql, stage.ExtractParameters(), ct);
        await session.Execute(command);
    }
    
    public async Task<Maybe<ParserWorkStage>> GetWorkStage(WorkStageQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string lockClause = query.LockClause();
        string sql = $"""
                      SELECT id, name, created, finished
                      FROM avito_parser_module.work_stages
                      {filterSql}
                      {lockClause}
                      LIMIT 1
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        NpgSqlParserWorkStageRow? row = await session.QueryMaybeRow<NpgSqlParserWorkStageRow>(command);
        return row.MaybeWorkStage();
    }
    
    public async Task<IEnumerable<ParserWorkStage>> GetWorkStages(WorkStageQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string lockClause = query.LockClause();
        string limitClause = query.LimitClause();
        string sql = $"""
                      SELECT id, name, created, finished
                      FROM avito_parser_module.work_stages
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        IEnumerable<NpgSqlParserWorkStageRow> rows = await session.QueryMultipleRows<NpgSqlParserWorkStageRow>(command);
        return rows.ToModels();
    }
}