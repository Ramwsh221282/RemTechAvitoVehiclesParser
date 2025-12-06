using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;

public sealed class RegisterParserCreationTicket(
    NpgSqlRegisteredTicketsStorage storage) :
    IRegisterParserCreationTicket
{
    private const string Type = ConstantsForMainApplicationCommunication.CreateParserRoutingKey;
    
    public async Task<RegisterParserServiceTicket> Handle(
        RegisterParserCreationTicketCommand command, 
        CancellationToken ct = default)
    {
        RegisterParserServiceTicket ticket = RegisterParserServiceTicket.New(
            ticketType: Type, 
            parserDomain: command.Domain, 
            parserType: command.Type);
        
        await storage.Store(ticket, ct);
        return ticket;
    }
}