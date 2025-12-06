using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;

public sealed class NpgSqlRegisteredTicketRow : ISnapshotSource<NpgSqlRegisteredTicketRow, RegisterParserServiceTicketSnapshot>
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Payload { get; init; }
    public required DateTime Created { get; init; }
    public required DateTime? Finished { get; init; }
    public required bool WasSent { get; init; }
    
    public RegisterParserServiceTicketSnapshot GetSnapshot()
    {
        return new RegisterParserServiceTicketSnapshot(Id, Type, Payload, Created, Finished, WasSent);
    }
}