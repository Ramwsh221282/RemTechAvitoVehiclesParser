using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

public sealed record RegisterParserServiceTicketSnapshot(
    Guid Id,
    string Type,
    string Payload,
    DateTime Created,
    DateTime? Finished,
    bool WasSent) : ISnapshot;