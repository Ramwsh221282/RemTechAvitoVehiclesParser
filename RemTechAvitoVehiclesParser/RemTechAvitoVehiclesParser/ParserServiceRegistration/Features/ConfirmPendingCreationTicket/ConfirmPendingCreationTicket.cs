using ParsingSDK.Parsing;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket;

public sealed class ConfirmPendingCreationTicket(
    NpgSqlRegisteredTicketsStorage storage
) :
    IConfirmPendingCreationTicket
{
    public async Task<RegisterParserServiceTicket> Handle(ConfirmPendingCreationTicketCommand command, CancellationToken ct = default)
    {
        QueryRegisteredTicketArgs args = new(Id: command.Id, WithLock: true);
        Maybe<RegisterParserServiceTicket> ticket = await storage.GetTicket(args: args, ct: ct);
        if (!ticket.HasValue) throw new InvalidOperationException($"Ticket with ID: {command.Id} does not exist.");
        RegisterParserServiceTicket finished = ticket.Value.Finish(DateTime.UtcNow);
        await storage.Clear(ct);
        return finished;
    }
}