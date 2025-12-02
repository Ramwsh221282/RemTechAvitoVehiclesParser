using Dapper;
using RemTechAvitoVehiclesParser.Models;

namespace RemTechAvitoVehiclesParser.Database;

public sealed class NpgSqlParserTicketsStorage(NpgSqlSession dbSession)
{
    public async Task Add(ParserTicket ticket, CancellationToken ct = default) =>
        await dbSession.Execute( c => ticket.SaveInDb().Invoke(c, dbSession.Transaction, ct), ct);

    public async Task Update(ParserTicket ticket, CancellationToken ct = default) =>
        await dbSession.Execute( c => ticket.UpdateInDb().Invoke(c, dbSession.Transaction, ct), ct);
    
    public async Task<ParserTicket[]> GetPendingTickets(CancellationToken ct = default, int limit = 50)
    {
        const string sql = """
                           SELECT id, type, payload, created, finished
                           FROM avito_parser_module.parser_tickets
                           WHERE finished IS NULL
                           LIMIT @limit
                           """;
        
        return await dbSession.Execute(async (c) =>
        {
            CommandDefinition command = new(sql, new { limit }, dbSession.Transaction, cancellationToken: ct);
            IEnumerable<NpgSqlParserTicket> tickets = await c.QueryAsync<NpgSqlParserTicket>(command);
            return tickets.Select(t => t.ToTicket()).ToArray();
        }, ct);
    }

    public async Task<int> RemoveProcessedTickets(CancellationToken ct = default)
    {
        const string sql = """
                           DELETE FROM avito_parser_module.parser_tickets
                           WHERE finished IS NOT NULL
                           """;
        
        return await dbSession.Execute(async c =>
        {
            CommandDefinition command = new (sql, dbSession.Transaction, cancellationToken: ct);
            return await c.ExecuteScalarAsync<int>(command);
        }, ct);
    }
    
    public async Task UpdateTickets(IEnumerable<ParserTicket> tickets, CancellationToken ct = default)
    {
        object parameters = new
        {
            finished = DateTime.UtcNow,
            ids = ParserTicket.ExtractIds(tickets)
        };
        
        const string sql = """
                           UPDATE avito_parser_module.parser_tickets
                           SET finished = @finished
                           WHERE id in @ids
                           """;

        CommandDefinition command = new(sql, parameters, cancellationToken: ct, transaction: dbSession.Transaction);
        await dbSession.Execute(c => c.ExecuteAsync(command), ct);
    }
    
    private sealed class NpgSqlParserTicket
    {
        public required Guid Id { get; init; }
        public required string Type { get; init; }
        public required string Payload { get; init; }
        public required DateTime Created { get; init; }
        public required DateTime?  Finished { get; init; }
        public ParserTicket ToTicket() => new(Id, Type, Payload, Created, Finished);
    }
}