using System.Data;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Extensions;

public static class CataloguePageUrlStoringImplementation
{
    extension(CataloguePageUrl)
    {
        public static async Task<CataloguePageUrl[]> GetMany(
            NpgSqlSession session,
            CataloguePageUrlQuery query,
            CancellationToken ct = default
        )
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string limitClause = query.LimitClause();
            string sql = $"""
                SELECT url, was_processed, retry_count
                FROM avito_parser_module.catalogue_urls
                {filterSql}
                {lockClause}
                {limitClause}
                """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<CataloguePageUrl> urls = [];
            while (reader.Read())
            {
                string url = reader.GetString(reader.GetOrdinal("url"));
                bool processed = reader.GetBoolean(reader.GetOrdinal("was_processed"));
                int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
                urls.Add(new CataloguePageUrl(url, processed, retryCount));
            }

            return [.. urls];
        }
    }

    extension(IEnumerable<CataloguePageUrl> urls)
    {
        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql = """
                UPDATE avito_parser_module.catalogue_urls
                SET was_processed = @processed,
                    retry_count = @retry_count
                WHERE url = @url;
                """;
            IEnumerable<object> parameters = urls.Select(l => l.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql = """
                INSERT INTO avito_parser_module.catalogue_urls
                (url, was_processed, retry_count)
                VALUES
                (@url, @processed, @retry_count) ON CONFLICT (url) DO NOTHING;
                """;
            IEnumerable<object> parameters = urls.Select(l => l.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(CataloguePageUrl url)
    {
        private object ExtractParameters() =>
            new
            {
                url = url.Url,
                processed = url.Processed,
                retry_count = url.RetryCount,
            };
    }

    extension(CataloguePageUrlQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filterSql = [];
            DynamicParameters parameters = new();

            if (query.ProcessedOnly)
                filterSql.Add("was_processed is TRUE");
            if (query.UnprocessedOnly)
                filterSql.Add("was_processed is FALSE");
            if (query.RetryLimit.HasValue)
            {
                filterSql.Add("retry_count <= @retry");
                parameters.Add("@retry", query.RetryLimit.Value, DbType.Int32);
            }

            return filterSql.Count == 0
                ? (parameters, "")
                : (parameters, $"WHERE {string.Join(" AND ", filterSql)}");
        }

        private string LockClause()
        {
            return query.WithLock ? "FOR UPDATE" : string.Empty;
        }

        private string LimitClause()
        {
            return query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
        }
    }
}
