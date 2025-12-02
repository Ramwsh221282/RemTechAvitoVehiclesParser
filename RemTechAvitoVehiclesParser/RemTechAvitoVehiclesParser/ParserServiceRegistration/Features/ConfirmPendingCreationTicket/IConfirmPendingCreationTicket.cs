using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket;

public interface IConfirmPendingCreationTicket
{
    Task<RegisterParserServiceTicketSnapshot> Handle(
        ConfirmPendingCreationTicketCommand command,
        CancellationToken ct = default);
}