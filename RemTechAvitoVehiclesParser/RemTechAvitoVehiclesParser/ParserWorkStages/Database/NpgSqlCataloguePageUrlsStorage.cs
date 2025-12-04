using System.Data;
using Dapper;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

// CREATE TABLE IF NOT EXISTS avito_parser_module.catalogue_urls
// (
//     id uuid primary key,
//     link_id uuid not null,
//     url text not null,
//     was_processed boolean not null,
//     retry_count integer not null,
//     CONSTRAINT link_id_fk FOREIGN KEY(link_id)
//     REFERENCES avito_parser_module.pagination_evaluating_parser_links(id)
//     ON DELETE CASCADE
// );
public sealed class NpgSqlCataloguePageUrlsStorage(IPostgreSqlAdapter adapter)
{
    public async Task<int> SaveMany<T>(IEnumerable<ISnapshotSource<T, CataloguePageUrlSnapshot>> catalogueUrlSnapshotSources)
        where T : class
    {
        const string sql = """
                           INSERT INTO avito_parser_module.catalogue_urls
                           (id, link_id, url, was_processed, retry_count)
                           VALUES
                           (@id, @link_id, @url, @was_processed, @retry_count)
                           """;
        CataloguePageUrlSnapshot[] snapshots = catalogueUrlSnapshotSources.Select(s => s.GetSnapshot()).ToArray();
        IEnumerable<object> parameters = snapshots.Select(CreateCatalogueUrlParameters);
        await adapter.ExecuteBulk(sql, parameters);
        return snapshots.Length;
    }
    
    public async Task<Maybe<CataloguePageUrl>> GetSingle(CataloguePageUrlQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = WhereClause(query);
        string lockClause = LockClause(query);
        string sql = $"""
                      SELECT id, link_id, url, was_processed, retry_count
                      FROM avito_parser_module.catalogue_urls
                      {filterSql}
                      {lockClause}
                      LIMIT 1
                      """;
        CommandDefinition command = adapter.CreateCommand(sql, parameters, ct);
        Maybe<NpgSqlCataloguePageUrlRow> row = await adapter.QuerySingle<NpgSqlCataloguePageUrlRow>(command, ct);
        return row.HasValue
            ? Maybe<CataloguePageUrl>.Some(row.Value.ToCataloguePageUrl())
            : Maybe<CataloguePageUrl>.None();
    }
    
    public async Task<IEnumerable<CataloguePageUrl>> GetMany(CataloguePageUrlQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = WhereClause(query);
        string lockClause = LockClause(query);
        string limitClause = LimitClause(query);
        string sql = $"""
                      SELECT id, link_id, url, was_processed, retry_count
                      FROM avito_parser_module.catalogue_urls
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = adapter.CreateCommand(sql, parameters, ct);
        IEnumerable<NpgSqlCataloguePageUrlRow> rows = await adapter.QueryMany<NpgSqlCataloguePageUrlRow>(command, ct);
        return rows.Select(r => r.ToCataloguePageUrl());
    }

    public async Task Update<T>(
        ISnapshotSource<T, CataloguePageUrlSnapshot> snapshotSource,
        CancellationToken ct = default)
        where T : class
    {
        const string sql = """
                           UPDATE avito_parser_module.catalogue_urls 
                           SET was_processed = @was_processed,
                               retry_count = @retry_count
                           WHERE id = @id;
                           """;
        CataloguePageUrlSnapshot snapshot = snapshotSource.GetSnapshot();
        CommandDefinition command = adapter.CreateCommand(sql, snapshot, CreateCatalogueUrlParameters, ct);
        await adapter.ExecuteCommand(command, ct);
    }
    
    private static string LimitClause(CataloguePageUrlQuery query)
    {
        return query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    }
    
    private static string LockClause(CataloguePageUrlQuery query)
    {
        return query.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    private static (DynamicParameters parameters, string filter) WhereClause(CataloguePageUrlQuery query)
    {
        List<string> filterSql = [];
        DynamicParameters parameters = new();

        if (query.LinkId.HasValue)
        {
            filterSql.Add("link_id = @link_id");
            parameters.Add("@link_id", query.LinkId.Value, DbType.Guid);
        }

        if (query.Id.HasValue)
        {
            filterSql.Add("id = @id");
            parameters.Add("@id", query.Id.Value, DbType.Guid);
        }

        if (query.ProcessedOnly.HasValue)
        {
            filterSql.Add("was_processed is true");
        }

        if (query.UnprocessedOnly.HasValue)
        {
            filterSql.Add("was_processed is false");
        }

        if (query.RetryLimitTreshold.HasValue)
        {
            filterSql.Add("retry_count < @treshold");
            parameters.Add("@treshold", query.RetryLimitTreshold.Value, DbType.Int32);
        }

        string sqlResult = filterSql.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filterSql);
        return (parameters, sqlResult);
    }
    
    private static object CreateCatalogueUrlParameters(CataloguePageUrlSnapshot snapshot)
    {
        return new
        {
            id = snapshot.Id,
            link_id = snapshot.LinkId,
            url = snapshot.Url,
            was_processed = snapshot.Processed,
            retry_count = snapshot.RetryCount
        };
    }
}