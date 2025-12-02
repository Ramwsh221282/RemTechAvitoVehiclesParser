using System.Data;
using Dapper;

namespace RemTechAvitoVehiclesParser.Models;

public delegate Task ParserTicketDatabaseOperation(
    IDbConnection connection,
    IDbTransaction? transaction = null,
    CancellationToken ct = default);

public sealed class ParserTicket(
    Guid id, 
    string type,
    string payload,
    DateTime created, 
    DateTime? finished)
{
    private readonly Guid _id = id;
    private readonly string _type = type;
    private readonly DateTime _created = created;
    private readonly DateTime? _finished = finished;
    private readonly string _payload = payload;
    
    private object MakeDbParameters()
    {
        return new
        {
            id = _id,
            type = _type,
            created = _created,
            finished = _finished,
            payload = _payload
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

    public static Guid[] ExtractIds(IEnumerable<ParserTicket> tickets)
    {
        return tickets.Select(t => t._id).ToArray();
    }
}