using System.Data;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing.Extensions;

public static class ProcessingParserLinkStoringImplementation
{
    extension(IEnumerable<ProcessingParserLink> links)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql = """
                INSERT INTO avito_parser_module.parser_links
                (id, url, was_processed, retry_count)
                VALUES
                (@id, @url, @was_processed, @retry_count);
                """;
            IEnumerable<object> parameters = links.Select(l => l.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql = """
                UPDATE avito_parser_module.parser_links
                SET was_processed = @was_processed,
                    retry_count = @retry_count
                WHERE id = @id;            
                """;
            IEnumerable<object> parameters = links.Select(l => l.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(ProcessingParserLink)
    {
        public static async Task<ProcessingParserLink[]> GetMany(
            NpgSqlSession session,
            ProcessingParserLinkQuery query,
            CancellationToken ct = default
        )
        {
            (DynamicParameters parameters, string filterSql) = WhereClause(query);
            string lockClause = LockClause(query);
            string sql = $"""
                    SELECT 
                    id as id, 
                    url as url, 
                    retry_count as retry_count, 
                    was_processed as was_processed
                    FROM avito_parser_module.parser_links
                    {filterSql}
                    {lockClause}                
                """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<ProcessingParserLink> result = [];
            while (reader.Read())
            {
                Guid id = reader.GetGuid(reader.GetOrdinal("id"));
                string url = reader.GetString(reader.GetOrdinal("url"));
                int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
                bool wasProcessed = reader.GetBoolean(reader.GetOrdinal("was_processed"));
                ProcessingParserLink link = new(id, url, wasProcessed, retryCount);
                result.Add(link);
            }

            return [.. result];
        }
    }

    extension(ProcessingParserLink link)
    {
        private object ExtractParameters() =>
            new
            {
                id = link.Id,
                url = link.Url,
                retry_count = link.RetryCount,
                was_processed = link.WasProcessed,
            };
    }

    extension(ProcessingParserLinkQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.ProcessedOnly)
                filters.Add("was_processed is TRUE");
            if (query.UnprocessedOnly)
                filters.Add("was_processed is FALSE");
            if (query.RetryLimit.HasValue)
            {
                filters.Add("retry_count <= @retry");
                parameters.Add("@retry", query.RetryLimit.Value, DbType.Int32);
            }

            return filters.Count == 0
                ? (parameters, "")
                : (parameters, $"WHERE {string.Join(" AND ", filters)}");
        }

        private string LockClause()
        {
            return query.WithLock ? "FOR UPDATE" : string.Empty;
        }
    }
}
