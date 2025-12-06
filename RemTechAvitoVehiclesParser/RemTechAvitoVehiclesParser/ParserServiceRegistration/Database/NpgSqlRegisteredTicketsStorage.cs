using Dapper;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;

// CREATE TABLE IF NOT EXISTS avito_parser_module.parser_tickets
// (
//     id uuid primary key,    
//     type varchar(256) not null,
//     payload jsonb not null,
//     created timestamptz not null,
//     was_sent boolean,
//     finished timestamptz
// );
public sealed class NpgSqlRegisteredTicketsStorage(NpgSqlSession session)
{
    public async Task<Maybe<RegisterParserServiceTicket>> GetTicket(QueryRegisteredTicketArgs args, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = args.WhereClause();
        string lockClause = args.LockClause();
        string sql = $"""
                     SELECT id, type, payload, created, was_sent, finished
                     FROM avito_parser_module.parser_tickets
                     {filterSql}
                     {lockClause}
                     LIMIT 1
                     """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct: ct);
        NpgSqlRegisteredTicketRow? row = await session.QueryMaybeRow<NpgSqlRegisteredTicketRow>(command);
        return row.MaybeTicket();
    }

    public async Task<IEnumerable<RegisterParserServiceTicket>> GetTickets(QueryRegisteredTicketArgs args, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = args.WhereClause();
        string limitClause = args.LimitClause();
        string lockClause = args.LockClause();
        string sql = $"""
                      SELECT id, type, payload, created, was_sent, finished
                      FROM avito_parser_module.parser_tickets
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct: ct);
        IEnumerable<NpgSqlRegisteredTicketRow> rows = await session.QueryMultipleRows<NpgSqlRegisteredTicketRow>(command);
        return rows.ToModels();
    }
    
    public async Task Update(RegisterParserServiceTicket ticket, CancellationToken ct = default)
    {
        const string sql = """
                           UPDATE avito_parser_module.parser_tickets
                           SET was_sent = @was_sent,
                               finished = @finished
                           WHERE id = @id
                           """;
        CommandDefinition command = session.FormCommand(sql, ticket.ExtractParameters(), ct);
        await session.Execute(command);
    }
    
    public async Task Store(RegisterParserServiceTicket ticket, CancellationToken ct = default)
    {
        const string sql = """
                           INSERT INTO avito_parser_module.parser_tickets
                           (id, type, payload, created, was_sent, finished)
                           VALUES
                           (@id, @type, @payload::jsonb, @created, @was_sent, @finished)
                           """;
        CommandDefinition command = session.FormCommand(sql, ticket.ExtractParameters(), ct);
        await session.Execute(command);
    }
    
    public async Task Clear(CancellationToken ct = default)
    {
        const string sql = "DELETE FROM avito_parser_module.parser_tickets";
        CommandDefinition command = new(sql, cancellationToken: ct);
        await session.Execute(command);
    }
    
    public async Task UpdateMany(IEnumerable<RegisterParserServiceTicket> tickets)
    {
        const string sql = """
                           UPDATE avito_parser_module.parser_tickets
                           SET was_sent = @was_sent,
                               finished = @finished
                           WHERE id = @id
                           """;
        IEnumerable<object> parameters = tickets.Select(t => t.ExtractParameters());
        await session.ExecuteBulk(sql, parameters);
    }
}