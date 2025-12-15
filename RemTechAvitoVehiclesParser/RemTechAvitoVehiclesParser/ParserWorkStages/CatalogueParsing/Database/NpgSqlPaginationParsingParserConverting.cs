using System.Data;
using Dapper;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Database;

public static class NpgSqlPaginationParsingParserConverting
{
    extension(PaginationParsingParser parser)
    {
        public object ExtractParameters() => new
        {
            id = parser.Id,
            domain = parser.Domain,
            type = parser.Type
        };
    }

    extension(PaginationParsingParserLink link)
    {
        public object ExtractParameters() => new
        {
            id = link.Id,
            parser_id = link.ParserId,
            url = link.Url,
            was_processed = link.WasProcessed,
            current_page = link.CurrentPage,
            max_page = link.MaxPage,
        };
    }

    extension(PaginationEvaluationParsersQuery args)
    {
        public (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (args.ParserId.HasValue)
            {
                filters.Add("l.parser_id = @parserId");
                parameters.Add("@parserId", args.ParserId.Value, DbType.Guid);
            }

            if (args.LinksWithoutCurrentPage) filters.Add("l.current_page is null");
            if (args.LinksWithoutMaxPage) filters.Add("l.max_page is null");
            if (args.LinksWithMaxPage) filters.Add("l.max_page is not null");
            if (args.LinksWithCurrentPage) filters.Add("l.current_page is not null");
            if (args.OnlyNotProcessedLinks) filters.Add("l.was_processed is FALSE");
            if (args.OnlyProcessedLinks) filters.Add("l.was_processed is TRUE");

            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }

        public string LinksLimitClause() => args.LinksLimit.HasValue ? $"LIMIT {args.LinksLimit.Value}" : string.Empty;

        public string LockClause() => args.WithLock ? "FOR UPDATE" : string.Empty;
    }
}