using System.Data;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Extensions;

public static class CataloguePageItemStoringImplementation
{
    extension(CataloguePageItem)
    {
        public static async Task<CataloguePageItem[]> GetMany(
            NpgSqlSession session,
            CataloguePageItemQuery query,
            CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string limitClause = query.LimitClause();
            string lockClause = query.LockClause();
            string sql =
            $"""
            SELECT
            id as id,
            url as url,
            was_processed as was_processed,
            retry_count as retry_count,
            payload as payload
            FROM avito_parser_module.catalogue_items
            {filterSql}
            {lockClause}
            {limitClause}
            """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<CataloguePageItem> result = [];
            while (reader.Read()) result.Add(CreateBy(reader));
            return [.. result];
        }

        private static CataloguePageItem CreateBy(IDataReader reader)
        {
            string id = reader.GetString(reader.GetOrdinal("id"));
            bool wasProcessed = reader.GetBoolean(reader.GetOrdinal("was_processed"));
            int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
            string payload = reader.GetString(reader.GetOrdinal("payload"));
            string url = reader.GetString(reader.GetOrdinal("url"));
            return new CataloguePageItem(id, url, payload, wasProcessed, retryCount);
        }
    }

    extension(IEnumerable<CataloguePageItem> item)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql =
            """
            INSERT INTO avito_parser_module.catalogue_items
            (id, url, was_processed, retry_count, payload)
            VALUES
            (@id, @url, @was_processed, @retry_count, @payload);
            """;
            IEnumerable<object> parameters = item.Select(i => i.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql =
            """
            UPDATE avito_parser_module.catalogue_items
            SET was_processed = @was_processed,
                retry_count = @retry_count,
                payload = @payload
            WHERE id = @id;
            """;
            IEnumerable<object> parameters = item.Select(i => i.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(CataloguePageItem item)
    {
        private object ExtractParameters() => new
        {
            id = item.Id,
            url = item.Url,
            was_processed = item.WasProcessed,
            retry_count = item.RetryCount,
            payload = item.Payload
        };
    }

    extension(CataloguePageItemQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.UnprocessedOnly) filters.Add("was_processed is FALSE");
            if (query.RetryCount.HasValue)
            {
                filters.Add("retry_count = @retry");
                parameters.Add("@retry", query.RetryCount.Value, DbType.Int32);
            }

            return (parameters, filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : "");
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : "";
        private string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit}" : "";
    }
}
