using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Database;

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
public sealed class NpgSqlPendingToPublishItemsStorage(NpgSqlSession session)
{
    public async Task SaveMany(IEnumerable<PendingToPublishItem> items)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.pending_items
                           (id, url, title, address, price, is_nds, description_list, characteristics, photos, was_processed)
                           VALUES
                           (@id, @url, @title, @address, @price, @is_nds, @description_list::jsonb, @characteristics::jsonb, @photos::jsonb, @was_processed)
                           ON CONFLICT (id) DO NOTHING
                           """;
        object[] parameters = items.Select(i => i.ExtractParameters()).ToArray();
        await session.ExecuteBulk(sql, parameters);
    }

    public async Task<IEnumerable<PendingToPublishItem>> GetMany(PendingItemsQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string lockClause = query.LockClause();
        string limitClause = query.LimitClause();
        string sql = $"""
                      SELECT id, url, title, address, price, is_nds, description_list, characteristics, photos, was_processed
                      FROM avito_parser_module.pending_items
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        IEnumerable<NpgSqlPendingItemRow> rows = await session.QueryMultipleRows<NpgSqlPendingItemRow>(command);
        return rows.ToModels();
    }

    public async Task UpdateMany(IEnumerable<PendingToPublishItem> items, CancellationToken ct = default)
    {
        const string sql = "UPDATE avito_parser_module.pending_items SET was_processed = @was_processed WHERE id = @id";
        object[] parameters = items.Select(i => i.ExtractParameters()).ToArray();
        await session.ExecuteBulk(sql, parameters);
    }
}