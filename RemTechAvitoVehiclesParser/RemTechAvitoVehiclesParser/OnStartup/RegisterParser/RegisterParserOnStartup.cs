using RemTechAvitoVehiclesParser.Database;
using RemTechAvitoVehiclesParser.Models;

namespace RemTechAvitoVehiclesParser.OnStartup.RegisterParser;

public sealed class RegisterParserOnStartup(NpgSqlParserTicketsStorage storage) : IRegisterParserOnStartup
{
    private readonly NpgSqlParserTicketsStorage _storage = storage;
    private const string Type = "register.parser";

    public async Task Invoke(string domain, string type)
    {
        ParserTicket ticket = new(Guid.NewGuid(), Type, DateTime.UtcNow, null);
        ticket.AddPayloadElement("domain", domain);
        ticket.AddPayloadElement("type", type);
        await _storage.Add(ticket);
    }
}