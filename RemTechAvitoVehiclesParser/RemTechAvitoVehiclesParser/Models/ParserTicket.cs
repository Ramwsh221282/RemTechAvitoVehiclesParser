using System.Data;
using System.Text.Json;
using Dapper;

namespace RemTechAvitoVehiclesParser.Models;

public delegate Task ParserTicketDatabaseOperation(
    IDbConnection connection,
    IDbTransaction? transaction = null,
    CancellationToken ct = default);

public sealed class ParserTicket(
    Guid id, 
    string type,
    DateTime created, 
    DateTime? finished)
{
    private readonly Dictionary<string, object> _payload = [];
    private readonly Guid _id = id;
    private readonly string _type = type;
    private readonly DateTime _created = created;
    private readonly DateTime? _finished = finished;
    private string _cachedPayload = string.Empty;
    
    private object MakeDbParameters()
    {
        return new
        {
            id = _id,
            type = _type,
            created = _created,
            finished = _finished,
            payload = Payload()
        };
    }
    
    public ParserTicketDatabaseOperation UpdateInDb()
    {
        return (conn, txn, ct) =>
        {
            object parameters = MakeDbParameters();

            const string sql = """
                               UPDATE avito_parser_module.parser_tickets
                               SET finished = @finished
                               WHERE id = @id
                               """;
            
            CommandDefinition command = new(sql, parameters, cancellationToken: ct, transaction: txn);
            return conn.ExecuteAsync(command);
        };
    }
    
    public ParserTicketDatabaseOperation SaveInDb()
    {
        return (conn, txn, ct) =>
        {
            object parameters = MakeDbParameters();
            
            const string sql = """
                               INSERT INTO avito_parser_module.parser_tickets
                               (id, type, payload, created, finished)
                               VALUES
                               (@id, @type, @payload::jsonb, @created, @finished)
                               """;
            
            CommandDefinition command = new(sql, parameters, cancellationToken: ct, transaction: txn);
            return conn.ExecuteAsync(command);
        };
    }
    
    public void AddPayloadElement(string key, object value) => _payload.Add(key, value);
     
    private string Payload()
    {
        if (!string.IsNullOrWhiteSpace(_cachedPayload)) return _cachedPayload;
        string payload = JsonSerializer.Serialize(_payload);
        _cachedPayload = payload;
        return _cachedPayload;
    }
}