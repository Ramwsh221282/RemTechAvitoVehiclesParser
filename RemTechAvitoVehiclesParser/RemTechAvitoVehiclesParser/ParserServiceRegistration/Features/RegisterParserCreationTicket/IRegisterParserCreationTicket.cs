using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;

public interface IRegisterParserCreationTicket
{
    Task<RegisterParserServiceTicket> Handle(RegisterParserCreationTicketCommand command, CancellationToken ct = default);
}