using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.PaginationParsing.Extensions;

public static class ProcessingParserStoringImplementation
{
    extension(ProcessingParser)
    {
        public static async Task<bool> HasAny(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
                SELECT COUNT(*) FROM avito_parser_module.parsers;
                """;
            CommandDefinition command = new(
                sql,
                cancellationToken: ct,
                transaction: session.Transaction
            );
            long count = await session.QuerySingleRow<long>(command);
            return count > 0;
        }
    }

    extension(ProcessingParser parser)
    {
        public async Task Persist(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
                INSERT INTO avito_parser_module.parsers
                (id, domain, type)
                VALUES
                (@id, @domain, @type)
                """;
            CommandDefinition command = session.FormCommand(sql, parser.ExtractParameters(), ct);
            await session.Execute(command);
        }

        private object ExtractParameters() =>
            new
            {
                id = parser.Id,
                domain = parser.Domain,
                type = parser.Type,
            };
    }
}
