namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;

public sealed record QueryRegisteredTicketArgs(
    Guid? Id = null,
    bool WithLock = false,
    bool FinishedOnly = false,
    bool SentOnly = false,
    bool NotSentOnly = false,
    bool NotFinishedOnly = false,
    int? Limit = null);