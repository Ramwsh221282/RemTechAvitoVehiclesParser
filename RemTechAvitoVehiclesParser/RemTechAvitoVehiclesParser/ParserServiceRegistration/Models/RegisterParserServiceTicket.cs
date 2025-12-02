using System.Text.Json;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

public sealed class RegisterParserServiceTicket(Guid id, string type, string payload, DateTime created, DateTime? finished, bool wasSent)
 : ISnapshotSource<RegisterParserServiceTicket, RegisterParserServiceTicketSnapshot>
{
    private readonly Guid _id = id;
    private readonly string _type = type;
    private readonly string _payload = payload;
    private readonly DateTime _created = created;
    private readonly DateTime? _finished = finished;
    private readonly bool _wasSent = wasSent;

    public RegisterParserServiceTicketSnapshot GetSnapshot()
    {
        InvokeValidation();
        return new(_id, _type, _payload, _created, _finished, _wasSent);
    }
    
    public RegisterParserServiceTicket MarkSent()
    {
        InvokeValidation();
        if (_wasSent) throw new InvalidOperationException(
            """
            Ticket was already sent.
            If you want to sent the ticket you have to create a new one.
            """);
        return new(this, wasSent: true);
    }

    public RegisterParserServiceTicket Finish(DateTime finishDate)
    {
        InvokeValidation();
        if (_finished.HasValue) throw new InvalidOperationException(
            """
            Ticket was already finished.
            If you want to finish the ticket you have to create a new one.                                                         
            """);
        return new(this, finished: finishDate);
    }

    public bool IsOfType(string type)
    {
        InvokeValidation();
        return _type == type;
    }

    public RegisterParserServiceTicket(Guid id, string type, object payload, DateTime created, DateTime? finished, bool wasSent)
        : this(id, type, JsonSerializer.Serialize(payload), created, finished, wasSent)
    { }

    public static RegisterParserServiceTicket FromSnapshot(RegisterParserServiceTicketSnapshot snapshot)
    {
        RegisterParserServiceTicket ticket = new(
            id: snapshot.Id,
            type: snapshot.Type,
            payload: snapshot.Payload,
            created: snapshot.Created,
            finished: snapshot.Finished,
            wasSent: snapshot.WasSent
        );
        ticket.InvokeValidation();
        return ticket;
    }
    
    public static RegisterParserServiceTicket New(
        string ticketType, 
        string parserDomain, 
        string parserType)
    {
        return new(
            id: Guid.NewGuid(), 
            type: ticketType, 
            payload: new { parser_domain = parserDomain, parser_type = parserType },
            created: DateTime.UtcNow,
            finished: null,
            wasSent: false);
    }
    
    private void InvokeValidation()
    {
        if (_id == Guid.Empty) throw new InvalidOperationException("Ticket id is not specified.");
        if (string.IsNullOrWhiteSpace(_type)) throw new InvalidOperationException("Ticket type is not specified.");
        if (string.IsNullOrWhiteSpace(_payload)) throw new InvalidOperationException("Ticket payload is not specified.");
        if (_created == DateTime.MinValue || _created == DateTime.MaxValue) throw new InvalidOperationException("Ticket creation time is invalid.");
        if (_finished.HasValue && (_finished.Value == DateTime.MinValue || _finished.Value == DateTime.MaxValue)) throw new InvalidOperationException("Ticket finished time is invalid.");   
    }
    
    private RegisterParserServiceTicket(
        RegisterParserServiceTicket origin,
        DateTime? created = null,
        DateTime? finished = null,
        bool? wasSent  = null
        ) : this(origin._id, 
        origin._type, 
        origin._payload, 
        created ?? origin._created, 
        finished ?? origin._finished, 
        wasSent ?? origin._wasSent) { }
}