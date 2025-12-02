using RemTechAvitoVehiclesParser.Models;

namespace RemTechAvitoVehiclesParser.Database;

public sealed class NpgSqlParserTicketsStorage(NpgSqlSession dbSession)
{
    public async Task Add(ParserTicket ticket, CancellationToken ct = default) =>
        await dbSession.Execute( c => ticket.SaveInDb().Invoke(c, dbSession.Transaction, ct), ct);

    public async Task Update(ParserTicket ticket, CancellationToken ct = default) =>
        await dbSession.Execute( c => ticket.UpdateInDb().Invoke(c, dbSession.Transaction, ct), ct);
}