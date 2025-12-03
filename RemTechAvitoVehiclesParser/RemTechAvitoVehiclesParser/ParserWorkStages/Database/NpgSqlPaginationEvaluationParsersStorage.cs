using System.Data;
using Dapper;
using RemTechAvitoVehiclesParser.ParserWorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.Database;

// CREATE TABLE IF NOT EXISTS avito_parser_module.pagination_evaluating_parsers
// (
//     id uuid primary key,
//     domain varchar(128) not null,
//     type varchar(128) not null
// );
//
// CREATE TABLE IF NOT EXISTS avito_parser_module.pagination_evaluating_parser_links
// (
//     id uuid primary key,
//     parser_id uuid not null,
//     url text not null,
//     was_processed boolean not null,
//     current_page integer,
//     max_page integer,
//     CONSTRAINT parser_id_fk FOREIGN KEY(parser_id)
//     REFERENCES avito_parser_module.pagination_evaluating_parsers
//     ON DELETE CASCADE 
//  );

public sealed class NpgSqlPaginationEvaluationParsersStorage(IPostgreSqlAdapter session)
{
    public async Task Save<T>(
        ISnapshotSource<T, PaginationEvaluationParserSnapshot> snapshotSource,
        CancellationToken ct = default,
        bool withLinks = false)
        where T : class
    {
        const string sql = """
                           INSERT INTO avito_parser_module.pagination_evaluating_parsers
                           (id, domain, type)
                           VALUES 
                           (@id, @domain, @type)
                           """;
        PaginationEvaluationParserSnapshot snapshot = snapshotSource.GetSnapshot();
        CommandDefinition command = session.CreateCommand(sql, snapshot, CreateParserParameters, ct);
        await session.ExecuteCommand(command, ct);
        if (withLinks) await SaveLinks(snapshot, ct);
    }
    
    public async Task<Maybe<PaginationEvaluationParser>> GetParser(
        PaginationEvaluationParsersQuery query,
        CancellationToken ct = default
        )
    {
        (DynamicParameters parameters, string filterSql) = WhereClause(query);
        string lockClause = LockClause(query);
        string sql = $"""
                           SELECT 
                           l.id as link_id,
                           l.parser_id as parser_id,
                           l.url as url,
                           l.was_processed as was_processed,
                           l.current_page as current_page,
                           l.max_page as max_page,
                           p.domain as domain,
                           p.type as type
                           FROM avito_parser_module.pagination_evaluating_parser_links l
                           INNER JOIN avito_parser_module.pagination_evaluating_parsers p ON l.parser_id = p.id
                           {filterSql}
                           {lockClause}
                           """;
        CommandDefinition command = session.CreateCommand(sql, parameters, ct);
        using IDataReader reader = await session.GetRowsReader(command, ct);
        Dictionary<Guid, PaginationEvaluationParser> readingScope = [];
        
        while (reader.Read())
        {
            Guid parserId = reader.GetGuid(reader.GetOrdinal("parser_id"));
            if (!readingScope.TryGetValue(parserId, out PaginationEvaluationParser? parser))
            {
                string parserDomain = reader.GetString(reader.GetOrdinal("domain"));
                string parserType = reader.GetString(reader.GetOrdinal("type"));
                parser = new(parserId, parserDomain, parserType, []);
                readingScope.Add(parserId, parser);
            }
            
            Guid linkId = reader.GetGuid(reader.GetOrdinal("link_id"));
            string linkUrl = reader.GetString(reader.GetOrdinal("url"));
            bool wasProcessed = reader.GetBoolean(reader.GetOrdinal("was_processed"));
            
            int currentPageOrdinal = reader.GetOrdinal("current_page");
            int? currentPage = reader.IsDBNull(currentPageOrdinal) ? null : reader.GetInt32(currentPageOrdinal);
            
            int maxPageOrdinal = reader.GetOrdinal("max_page");
            int? maxPage = reader.IsDBNull(maxPageOrdinal) ? null : reader.GetInt32(maxPageOrdinal);
            
            PaginationEvaluationParserLink link = new(linkId, linkUrl, wasProcessed, currentPage, maxPage);
            parser.AddLink(link);
        }

        return readingScope.Count == 0
            ? Maybe<PaginationEvaluationParser>.None()
            : Maybe<PaginationEvaluationParser>.Some(readingScope.First().Value);
    }
    
    public async Task UpdateLink<T>(
        ISnapshotSource<T, PaginationEvaluationParserLinkSnapshot> linkSnapshotSource,
        Guid parserId,
        CancellationToken ct = default)
        where T : class
    {
        const string sql = """
                           UPDATE avito_parser_module.pagination_evaluating_parser_links
                           SET was_processed = @was_processed,
                               current_page = @current_page,
                               max_page = @max_page
                           WHERE id = @id
                           """;
        PaginationEvaluationParserLinkSnapshot linkSnapshot = linkSnapshotSource.GetSnapshot();
        object parameters = CreateParserLinkParameters(linkSnapshot, parserId);
        CommandDefinition command = session.CreateCommand(sql, parameters, ct);
        await session.ExecuteCommand(command, ct);
    }
    
    public async Task UpdateLink<T>(
        ISnapshotSource<T, PaginationEvaluationParserLinkSnapshot> linkSnapshotSource,
        ISnapshotSource<T, PaginationEvaluationParserSnapshot> parserSnapshotSource,
        CancellationToken ct = default)
        where T : class => await UpdateLink(linkSnapshotSource, parserSnapshotSource.GetSnapshot().Id, ct);
    
    private async Task SaveLinks(PaginationEvaluationParserSnapshot snapshot, CancellationToken ct)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.pagination_evaluating_parser_links
                           (id, parser_id, url, was_processed, current_page, max_page)
                           VALUES
                           (@id, @parser_id, @url, @was_processed, @current_page, @max_page)
                           """;
        IEnumerable<object> linkParameters = snapshot.Links.Select(l => CreateParserLinkParameters(l, snapshot));
        await session.ExecuteBulk(sql, linkParameters);
    }

    private static string LockClause(PaginationEvaluationParsersQuery query)
    {
        return query.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    private static (DynamicParameters parameters, string filterSql) WhereClause(PaginationEvaluationParsersQuery query)
    {
        List<string> filters = [];
        DynamicParameters parameters = new();

        if (query.ParserId.HasValue)
        {
            filters.Add("l.parser_id = @parserId");
            parameters.Add("@parserId", query.ParserId.Value, DbType.Guid);
        }

        if (query.LinksWithoutCurrentPage)
        {
            filters.Add("l.current_page is null");
        }

        if (query.LinksWithoutMaxPage)
        {
            filters.Add("l.max_page is null");
        }

        if (query.LinksWithMaxPage)
        {
            filters.Add("l.max_page is not null");
        }

        if (query.LinksWithCurrentPage)
        {
            filters.Add("l.current_page is not null");
        }

        string sql = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        return (parameters, sql);
    }
    
    private static object CreateParserParameters(PaginationEvaluationParserSnapshot snapshot)
    {
        return new
        {
            id = snapshot.Id,
            domain = snapshot.Domain,
            type = snapshot.Type
        };
    }
        
    private static object CreateParserLinkParameters(PaginationEvaluationParserLinkSnapshot linkSnapshot, Guid parserId)
    {
        return new
        {
            id = linkSnapshot.Id,
            parser_id = parserId,
            url = linkSnapshot.Url,
            was_processed = linkSnapshot.WasProcessed,
            current_page = linkSnapshot.CurrentPage,
            max_page = linkSnapshot.MaxPage,
        };
    }
    
    private static object CreateParserLinkParameters(
        PaginationEvaluationParserLinkSnapshot linkSnapshot, 
        PaginationEvaluationParserSnapshot parserSnapshot)
    {
        return new
        {
            id = linkSnapshot.Id,
            parser_id = parserSnapshot.Id,
            url = linkSnapshot.Url,
            was_processed = linkSnapshot.WasProcessed,
            current_page = linkSnapshot.CurrentPage,
            max_page = linkSnapshot.MaxPage,
        };
    }
}