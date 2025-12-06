using System.Text.Json;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

public sealed record RegisterParserServiceTicket(
    Guid Id,
    string Type,
    string Payload,
    DateTime Created,
    DateTime? Finished,
    bool WasSent);

public static class RegisterParserServiceTicketImplementation
{
    extension(RegisterParserServiceTicket ticket)
    {
        public RegisterParserServiceTicket MarkSent()
        {
            if (ticket.WasSent) throw new InvalidOperationException(
                """
                Ticket was already sent.
                If you want to sent the ticket you have to create a new one.
                """);
            return ticket with { WasSent = true };
        }

        public RegisterParserServiceTicket Finish(DateTime finishDate)
        {
            if (ticket.Finished.HasValue) throw new InvalidOperationException(
                """
                Ticket was already finished.
                If you want to finish the ticket you have to create a new one.                                                         
                """);
            return ticket with { Finished =  finishDate };
        }
        
        public bool IsOfType(string type)
        {
            return ticket.Type == type;
        }
    }
}

public static class RegisterParserServiceTicketConstruction
{
    extension(RegisterParserServiceTicket)
    {
        public static RegisterParserServiceTicket From<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, string> typeMap,
            Func<T, string> payloadMap,
            Func<T, DateTime> createdMap,
            Func<T, DateTime?> finishedMap,
            Func<T, bool> wasSentMap
            ) 
            where T : class
        {
            return new(
                Id: idMap(source),
                Type: typeMap(source),
                Payload: payloadMap(source),
                Created: createdMap(source),
                Finished: finishedMap(source),
                WasSent: wasSentMap(source)
                );
        }
        
        public static RegisterParserServiceTicket From<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, string> typeMap,
            Func<T, object> payloadMap,
            Func<T, DateTime> createdMap,
            Func<T, DateTime?> finishedMap,
            Func<T, bool> wasSentMap
        ) 
            where T : class
        {
            return new(
                Id: idMap(source),
                Type: typeMap(source),
                Payload: JsonSerializer.Serialize(payloadMap(source)),
                Created: createdMap(source),
                Finished: finishedMap(source),
                WasSent: wasSentMap(source)
            );
        }
        
        public static RegisterParserServiceTicket New(
            string ticketType, 
            string parserDomain, 
            string parserType)
        {
            object payloadData = new { parser_domain = parserDomain, parser_type = parserType };
            string payload = JsonSerializer.Serialize(payloadData);
            return New(ticketType, payload);
        }

        public static RegisterParserServiceTicket New(
            string ticketType,
            string payload)
        {
            return new(
                Id: Guid.NewGuid(), 
                Type: ticketType, 
                Payload: payload,
                Created: DateTime.UtcNow,
                Finished: null,
                WasSent: false
            );
        }   
    }
}