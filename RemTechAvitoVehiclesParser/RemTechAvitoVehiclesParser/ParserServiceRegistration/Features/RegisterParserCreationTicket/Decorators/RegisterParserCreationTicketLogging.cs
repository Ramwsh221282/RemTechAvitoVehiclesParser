using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.RegisterParserCreationTicket.Decorators;

public sealed class RegisterParserCreationTicketLogging(
    Serilog.ILogger logger, 
    IRegisterParserCreationTicket origin) 
    : IRegisterParserCreationTicket
{
    private readonly Serilog.ILogger _logger = logger.ForContext<RegisterParserCreationTicketLogging>();
    
    public async Task<RegisterParserServiceTicket> Handle(RegisterParserCreationTicketCommand command, CancellationToken ct = default)
    {
        try
        {
            RegisterParserServiceTicket ticket = await origin.Handle(command, ct);
            
            _logger.Information(
                """
                Registered parser creation ticket:
                Id: {Id}
                Payload: {Payload}
                Type: {Type}  
                """,
                ticket.Id,
                ticket.Payload,
                ticket.Type);
            
            return ticket;
        }
        catch(Exception ex)
        {
            _logger.Error(ex, "Unable to register parser creation ticket");
            throw;
        }
    }
}