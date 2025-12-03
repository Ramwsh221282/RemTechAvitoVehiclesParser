using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket.Decorators;

public sealed class ConfirmPendingCreationTicketTransaction(
    IPostgreSqlAdapter session,
    IConfirmPendingCreationTicket origin
) :
    IConfirmPendingCreationTicket
{
    public async Task<RegisterParserServiceTicketSnapshot> Handle(ConfirmPendingCreationTicketCommand command, CancellationToken ct = default)
    {
        await session.UseTransaction(ct);
        RegisterParserServiceTicketSnapshot result = await origin.Handle(command, ct);
        await session.CommitTransaction(ct);
        return result;
    }
}