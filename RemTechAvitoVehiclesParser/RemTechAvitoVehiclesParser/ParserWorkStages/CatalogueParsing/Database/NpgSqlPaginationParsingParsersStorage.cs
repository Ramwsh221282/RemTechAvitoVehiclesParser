using System.Data;
using Dapper;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;

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

public sealed class NpgSqlPaginationParsingParsersStorage(NpgSqlSession session)
{
    public async Task Save(
        PaginationParsingParser parser,
        CancellationToken ct = default,
        bool withLinks = false)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.pagination_evaluating_parsers
                           (id, domain, type)
                           VALUES 
                           (@id, @domain, @type)
                           """;
        CommandDefinition command = session.FormCommand(sql, parser.ExtractParameters(), ct);
        await session.Execute(command);
        if (withLinks) await SaveLinks(parser);
    }

    public async Task<Maybe<PaginationParsingParser>> GetParser(PaginationEvaluationParsersQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string lockClause = query.LockClause();
        string linksLimitClause = query.LinksLimitClause();
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
                           {linksLimitClause}
                           """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        using IDataReader reader = await session.ExecuteReader(command, ct);
        Dictionary<Guid, PaginationParsingParser> readingScope = [];

        while (reader.Read())
        {
            Guid parserId = reader.GetGuid(reader.GetOrdinal("parser_id"));
            if (!readingScope.TryGetValue(parserId, out PaginationParsingParser? parser))
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

            PaginationParsingParserLink link = new(linkId, parserId, linkUrl, wasProcessed, currentPage, maxPage);
            readingScope[parserId] = parser.AddLink(link);
        }

        return readingScope.Count == 0 ? Maybe<PaginationParsingParser>.None() : Maybe<PaginationParsingParser>.Some(readingScope.First().Value);
    }

    public async Task UpdateLink(PaginationParsingParserLink link, CancellationToken ct = default)
    {
        const string sql = """
                           UPDATE avito_parser_module.pagination_evaluating_parser_links
                           SET was_processed = @was_processed,
                               current_page = @current_page,
                               max_page = @max_page
                           WHERE id = @id
                           """;
        CommandDefinition command = session.FormCommand(sql, link.ExtractParameters(), ct);
        await session.Execute(command);
    }

    public async Task UpdateManyLinks(IEnumerable<PaginationParsingParserLink> links)
    {
        const string sql =
        """
        UPDATE avito_parser_module.pagination_evaluating_parser_links
        SET was_processed = @was_processed,
        current_page = @current_page,
        max_page = @max_page
        WHERE id = @id 
        """;
        IEnumerable<object> parameters = links.Select(l => l.ExtractParameters());
        await session.ExecuteBulk(sql, parameters);
    }

    private async Task SaveLinks(PaginationParsingParser parser)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.pagination_evaluating_parser_links
                           (id, parser_id, url, was_processed, current_page, max_page)
                           VALUES
                           (@id, @parser_id, @url, @was_processed, @current_page, @max_page)
                           """;
        await session.ExecuteBulk(sql, parser.Links.Select(l => l.ExtractParameters()));
    }
}