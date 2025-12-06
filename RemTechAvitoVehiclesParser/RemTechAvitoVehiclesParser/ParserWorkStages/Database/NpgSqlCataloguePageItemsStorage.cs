using System.Data;
using Dapper;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

// CREATE TABLE IF NOT EXISTS avito_parser_module.catalogue_items
// (
//     id uuid primary key,
//     catalogue_url_id uuid not null,
//     was_processed boolean not null,
//     retry_count integer not null,
//     payload jsonb not null,
//     CONSTRAINT catalogue_url_id FOREIGN KEY(catalogue_url_id)
//     REFERENCES avito_parser_module.catalogue_urls(id)
//     ON DELETE CASCADE 
// );
public sealed class NpgSqlCataloguePageItemsStorage(IPostgreSqlAdapter adapter)
{
    public async Task<int> InsertMany(IEnumerable<CataloguePageItem> items)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.catalogue_items
                           (id, catalogue_url_id, was_processed, retry_count, payload)
                           VALUES
                           (@id, @catalogue_url_id, @was_processed, @retry_count, @payload::jsonb)
                           ON CONFLICT (id) DO NOTHING;
                           """;
        object[] parameters = items.Select(u => CreateParameters(u.GetSnapshot())).ToArray();
        await adapter.ExecuteBulk(sql, parameters);
        return parameters.Length;
    }
    
    public async Task<int> UpdateMany(IEnumerable<CataloguePageItem> items)
    {
        const string sql = """
                           UPDATE avito_parser_module.catalogue_items
                           SET
                               was_processed = @was_processed,
                               retry_count = @retry_count
                           WHERE
                               id = @id
                           """;
        object[] parameters = items.Select(u => CreateParameters(u.GetSnapshot())).ToArray();
        await adapter.ExecuteBulk(sql, parameters);
        return parameters.Length;
    }

    public async Task<IEnumerable<CataloguePageItem>> GetItems(CataloguePageItemQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = WhereClause(query);
        string lockClause = LockClause(query);
        string limitClause = LimitClause(query);
        string sql = $"""
                      SELECT id, catalogue_url_id, was_processed, retry_count, payload
                      FROM avito_parser_module.catalogue_items
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = adapter.CreateCommand(sql, parameters, ct);
        IEnumerable<NpgSqlCataloguePageItemRow> rows = await adapter.QueryMany<NpgSqlCataloguePageItemRow>(command, ct);
        return rows.Select(r => r.ToCataloguePageItem());
    }

    private static string LimitClause(CataloguePageItemQuery query)
    {
        return query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    }
    
    private static string LockClause(CataloguePageItemQuery query)
    {
        return query.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    private static (DynamicParameters parameters, string filterSql) WhereClause(CataloguePageItemQuery query)
    {
        List<string> filters = [];
        DynamicParameters parameters = new();

        if (query.Id.HasValue)
        {
            filters.Add("id=@id");
            parameters.Add("@id", query.Id.Value, DbType.Guid);
        }

        if (query.ProcessedOnly)
        {
            filters.Add("was_processed is true");
        }

        if (query.NotProcessedOnly)
        {
            filters.Add("was_processed is false");
        }

        if (query.RetryLimitTreshold.HasValue)
        {
            filters.Add("retry_count < @retry_count");
            parameters.Add("@retry_count", query.RetryLimitTreshold.Value, DbType.Int32);
        }

        string filterSql = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        return (parameters, filterSql);
    }
    
    private static object CreateParameters(CataloguePageItemSnapshot snapshot)
    {
        return new
        {
            id = snapshot.Id,
            catalogue_url_id = snapshot.CatalogueUrlId,
            was_processed = snapshot.WasProcessed,
            retry_count = snapshot.RetryCount,
            payload = snapshot.Payload,
        };
    }
}