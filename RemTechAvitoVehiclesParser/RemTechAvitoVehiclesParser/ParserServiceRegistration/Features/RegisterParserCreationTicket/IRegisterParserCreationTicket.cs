using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;

public interface IRegisterParserCreationTicket
{
    Task<RegisterParserServiceTicketSnapshot> Handle(
        RegisterParserCreationTicketCommand command, 
        CancellationToken ct = default);
}