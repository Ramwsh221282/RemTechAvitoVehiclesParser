using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket.Decorators;

public sealed class RegisterParserCreationTicketTransaction(
    NpgSqlSession session, 
    IRegisterParserCreationTicket origin) : 
    IRegisterParserCreationTicket
{
    public async Task<RegisterParserServiceTicketSnapshot> Handle(
        RegisterParserCreationTicketCommand command, 
        CancellationToken ct = default)
    {
        await session.UseTransaction(ct);
        RegisterParserServiceTicketSnapshot result = await origin.Handle(command, ct);
        await session.CommitTransaction(ct);
        return result;
    }
}