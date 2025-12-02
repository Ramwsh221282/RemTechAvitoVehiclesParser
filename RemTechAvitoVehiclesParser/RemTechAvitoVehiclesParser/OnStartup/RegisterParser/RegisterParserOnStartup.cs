using System.Text.Json;
using RemTechAvitoVehiclesParser.Database;
using RemTechAvitoVehiclesParser.Models;

namespace RemTechAvitoVehiclesParser.OnStartup.RegisterParser;

public sealed class RegisterParserOnStartup(NpgSqlParserTicketsStorage storage) : IRegisterParserOnStartup
{
    private readonly NpgSqlParserTicketsStorage _storage = storage;
    private const string Type = "register.parser";

    public async Task Invoke(string domain, string type)
    {
        object payload = new { domain, type };
        string json = JsonSerializer.Serialize(payload);
        ParserTicket ticket = new(Guid.NewGuid(), Type, json, DateTime.UtcNow, null);
        await _storage.Add(ticket);
    }
}