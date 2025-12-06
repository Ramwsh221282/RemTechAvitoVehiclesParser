using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket.Decorators;

public sealed class ConfirmPendingCreationTicketLogging(
    Serilog.ILogger logger,
    IConfirmPendingCreationTicket origin
) :
    IConfirmPendingCreationTicket
{
    private readonly Serilog.ILogger _logger = logger.ForContext<IConfirmPendingCreationTicket>();
    
    public async Task<RegisterParserServiceTicket> Handle(ConfirmPendingCreationTicketCommand command, CancellationToken ct = default)
    {
        _logger.Information("Confirming pending creation ticket with ID: {Id}", command.Id);
        try
        {
            RegisterParserServiceTicket snapshot = await origin.Handle(command, ct);
            string finishedDateInfo = snapshot.Finished.HasValue ? snapshot.Finished.Value.ToString("dd-MM-yy") : "no finish date";
            string createdDateInfo = snapshot.Created.ToString("dd-MM-yy");
            
            _logger.Information("""
                                Pending creation ticket confirmed:
                                ID: {Id}
                                Date Created: {Created}
                                Date Finished: {Finished}
                                Was sent: {WasSent}
                                Payload: {Payload}
                                """, 
                                snapshot.Id, 
                                createdDateInfo, 
                                finishedDateInfo, 
                                snapshot.WasSent, 
                                snapshot.Payload);
            
            return snapshot;
        }
        catch(Exception ex)
        {
            _logger.Error(ex, "Error at confirming pending creation ticket with ID: {Id}.", command.Id);
            throw;
        }
    }
}