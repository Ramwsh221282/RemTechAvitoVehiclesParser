using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;

public sealed class NpgSqlRegisteredTicketRow
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public required DateTime Created { get; init; }
    public required DateTime? Finished { get; init; }
    public required bool WasSent { get; init; }
    
    public RegisterParserServiceTicket ToModel()
    {
        return RegisterParserServiceTicket.From(
            this,
            idMap: d => d.Id,
            typeMap: d => d.Type,
            payloadMap: d => d.Payload,
            createdMap: d => d.Created,
            finishedMap: d => d.Finished,
            d => d.WasSent
        );
    }
}