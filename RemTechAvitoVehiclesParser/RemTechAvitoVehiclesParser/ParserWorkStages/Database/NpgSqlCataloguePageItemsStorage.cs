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