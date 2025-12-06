using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket.Decorators;

public sealed class RegisterParserCreationTicketTransaction(
    NpgSqlSession session, 
    IRegisterParserCreationTicket origin) : 
    IRegisterParserCreationTicket
{
    public async Task<RegisterParserServiceTicket> Handle(
        RegisterParserCreationTicketCommand command, 
        CancellationToken ct = default)
    {
        await session.UseTransaction(ct);
        RegisterParserServiceTicket result = await origin.Handle(command, ct);
        await session.UnsafeCommit(ct);
        return result;
    }
}