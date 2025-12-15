using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;

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
public sealed class NpgSqlCataloguePageItemsStorage(NpgSqlSession adapter)
{
    public async Task InsertMany(IEnumerable<CataloguePageItem> items)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.catalogue_items
                           (id, catalogue_url_id, was_processed, retry_count, payload)
                           VALUES
                           (@id, @catalogue_url_id, @was_processed, @retry_count, @payload::jsonb)
                           ON CONFLICT (id) DO NOTHING;
                           """;
        object[] parameters = items.Select(u => u.ExtractParameters()).ToArray();
        await adapter.ExecuteBulk(sql, parameters);
    }

    public async Task UpdateMany(IEnumerable<CataloguePageItem> items)
    {
        const string sql = """
                           UPDATE avito_parser_module.catalogue_items
                           SET
                               was_processed = @was_processed,
                               retry_count = @retry_count
                           WHERE
                               id = @id
                           """;
        object[] parameters = items.Select(u => u.ExtractParameters()).ToArray();
        await adapter.ExecuteBulk(sql, parameters);
    }

    public async Task<IEnumerable<CataloguePageItem>> GetItems(CataloguePageItemQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string lockClause = query.LockClause();
        string limitClause = query.LimitClause();
        string sql = $"""
                      SELECT id, catalogue_url_id, was_processed, retry_count, payload
                      FROM avito_parser_module.catalogue_items
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = adapter.FormCommand(sql, parameters, ct);
        IEnumerable<NpgSqlCataloguePageItemRow> rows = await adapter.QueryMultipleRows<NpgSqlCataloguePageItemRow>(command);
        return rows.ToModels();
    }
}