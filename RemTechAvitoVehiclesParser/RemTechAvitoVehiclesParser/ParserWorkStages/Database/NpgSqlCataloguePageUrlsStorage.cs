using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
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
// REFERENCES avito_parser_module.pagination_evaluating_parser_links(id)
// ON DELETE CASCADE
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