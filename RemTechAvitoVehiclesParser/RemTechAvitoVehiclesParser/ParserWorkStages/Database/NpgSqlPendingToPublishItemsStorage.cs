using System.Text.Json;
using Dapper;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

// CREATE TABLE IF NOT EXISTS avito_parser_module.pending_items
// (
//     id varchar(32) primary key,
//     url text not null,
//     title varchar(128) not null,
//     address varchar(512) not null,
//     price bigint not null,
//     is_nds boolean not null,
//     description_list jsonb not null,
//     characteristics jsonb not null,
//     photos jsonb not null,
//     was_processed boolean not null
// );

public sealed class NpgSqlPendingToPublishItemsStorage(IPostgreSqlAdapter adapter)
{
    public async Task<int> SaveMany(IEnumerable<PendingToPublishItem> items)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.pending_items
                           (id, url, title, address, price, is_nds, description_list, characteristics, photos, was_processed)
                           VALUES
                           (@id, @url, @title, @address, @price, @is_nds, @description_list::jsonb, @characteristics::jsonb, @photos::jsonb, @was_processed)
                           ON CONFLICT (id) DO NOTHING
                           """;
        object[] parameters = items.Select(i => CreateParameters(i.GetSnapshot())).ToArray();
        await adapter.ExecuteBulk(sql, parameters);
        return parameters.Length;
    }

    public async Task<IEnumerable<PendingToPublishItem>> GetMany(PendingItemsQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = WhereClause(query);
        string lockClause = LockClause(query);
        string limitClause = LimitClause(query);
        string sql = $"""
                      SELECT id, url, title, address, price, is_nds, description_list, characteristics, photos, was_processed
                      FROM avito_parser_module.pending_items
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = adapter.CreateCommand(sql, parameters, ct);
        IEnumerable<NpgSqlPendingItemRow> rows = await adapter.QueryMany<NpgSqlPendingItemRow>(command, ct);
        return rows.Select(row => row.PendingItem());
    }

    public async Task UpdateMany(IEnumerable<PendingToPublishItem> items, CancellationToken ct = default)
    {
        const string sql = """
                           UPDATE avito_parser_module.pending_items
                           SET was_processed = @was_processed
                           WHERE id = @id
                           """;
        object[] parameters = items.Select(i => CreateParameters(i.GetSnapshot())).ToArray();
        await adapter.ExecuteBulk(sql, parameters);
    }
    
    private static string LockClause(PendingItemsQuery query)
    {
        return query.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    private static string LimitClause(PendingItemsQuery query)
    {
        return query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    }
    
    private static (DynamicParameters, string filterSql) WhereClause(PendingItemsQuery query)
    {
        List<string> filters = [];
        DynamicParameters parameters = new();

        if (query.UnprocessedOnly)
        {
            filters.Add("was_processed is FALSE");
        }
        
        string sql = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        return (parameters, sql);
    }
    
    private static object CreateParameters(PendingToPublishItemSnapshot snapshot) => new
    {
        id = snapshot.Id,
        url = snapshot.Url,
        title = snapshot.Title,
        address = snapshot.Address,
        price = snapshot.Price,
        is_nds = snapshot.IsNds,
        description_list = JsonSerializer.Serialize(snapshot.DescriptionList),
        characteristics = JsonSerializer.Serialize(snapshot.Characteristics),
        photos = JsonSerializer.Serialize(snapshot.Photos),
        was_processed = snapshot.WasProcessed
    };
}