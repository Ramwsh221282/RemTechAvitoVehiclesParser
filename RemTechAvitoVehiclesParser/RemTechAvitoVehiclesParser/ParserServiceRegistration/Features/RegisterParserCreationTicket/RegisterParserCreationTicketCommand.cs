namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket;

public sealed record RegisterParserCreationTicketCommand(
    string Domain,
    string Type);