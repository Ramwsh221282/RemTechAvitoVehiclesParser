using Dapper;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;
using System.Data;
namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;

public static class PostgreSqlWorkStageStorageExtension
{
    extension(ParserWorkStage)
    {
        public static async Task<Maybe<ParserWorkStage>> GetSingle(
            NpgSqlSession session,
            WorkStageQuery query,
            CancellationToken ct = default
        )
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string sql =
            $"""
            SELECT 
            id as id, 
            name as name
            FROM avito_parser_module.work_stages 
            {filterSql}
            {lockClause}
            LIMIT 1
            """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            while (reader.Read())
            {
                Guid id = reader.GetGuid(reader.GetOrdinal("id"));
                string name = reader.GetString(reader.GetOrdinal("name"));
                ParserWorkStage stage = ParserWorkStage.Create(id, name);
                return Maybe<ParserWorkStage>.Some(stage);
            }

            return Maybe<ParserWorkStage>.None();
        }
    }

    extension(ParserWorkStage stage)
    {
        public async Task Update(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql =
            """
            UPDATE avito_parser_module.work_stages 
            SET name = @name 
            WHERE id = @id
            """;
            CommandDefinition command = session.FormCommand(sql, stage.Parameters, ct);
            await session.Execute(command);
        }

        public async Task Persist(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql =
            """
            INSERT INTO avito_parser_module.work_stages(id, name)
            VALUES(@id, @name)
            """;
            CommandDefinition command = session.FormCommand(sql, stage.Parameters, ct);
            await session.Execute(command);
        }

        private object Parameters => new
        {
            id = stage.Id,
            name = stage.Name,
        };
    }

    extension(WorkStageQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
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

            string resultSql = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
            return (parameters, resultSql);
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
        private string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit}" : string.Empty;
    }
}
