using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket.Decorators;

public sealed class ConfirmPendingCreationTicketTransaction(
    NpgSqlSession session,
    IConfirmPendingCreationTicket origin
) :
    IConfirmPendingCreationTicket
{
    public async Task<RegisterParserServiceTicket> Handle(ConfirmPendingCreationTicketCommand command, CancellationToken ct = default)
    {
        await session.UseTransaction(ct);
        RegisterParserServiceTicket result = await origin.Handle(command, ct);
        await session.UnsafeCommit(ct);
        return result;
    }
}