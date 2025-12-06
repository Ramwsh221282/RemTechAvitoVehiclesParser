using Dapper;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.ConcreteItemParsing.Database;

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
public sealed class NpgSqlCataloguePageUrlsStorage(NpgSqlSession session)
{
    public async Task SaveMany(IEnumerable<CataloguePageUrl> urls)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.catalogue_urls
                           (id, link_id, url, was_processed, retry_count)
                           VALUES
                           (@id, @link_id, @url, @was_processed, @retry_count)
                           """;
        IEnumerable<object> parameters = urls.Select(u => u.ExtractParameters());
        await session.ExecuteBulk(sql, parameters);
    }
    
    public async Task<Maybe<CataloguePageUrl>> GetSingle(CataloguePageUrlQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string lockClause = query.LockClause();
        string sql = $"""
                      SELECT id, link_id, url, was_processed, retry_count
                      FROM avito_parser_module.catalogue_urls
                      {filterSql}
                      {lockClause}
                      LIMIT 1
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        NpgSqlCataloguePageUrlRow? row = await session.QueryMaybeRow<NpgSqlCataloguePageUrlRow>(command);
        return row.MaybeUrl();
    }
    
    public async Task<IEnumerable<CataloguePageUrl>> GetMany(CataloguePageUrlQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string lockClause = query.LockClause();
        string limitClause = query.LimitClause();
        string sql = $"""
                      SELECT id, link_id, url, was_processed, retry_count
                      FROM avito_parser_module.catalogue_urls
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        IEnumerable<NpgSqlCataloguePageUrlRow> rows = await session.QueryMultipleRows<NpgSqlCataloguePageUrlRow>(command);
        return rows.ToModels();
    }

    public async Task Update(CataloguePageUrl url, CancellationToken ct = default)
    {
        const string sql = """
                           UPDATE avito_parser_module.catalogue_urls 
                           SET was_processed = @was_processed,
                               retry_count = @retry_count
                           WHERE id = @id;
                           """;
        CommandDefinition command = session.FormCommand(sql, url.ExtractParameters(), ct);
        await session.Execute(command);
    }
}