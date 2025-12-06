using System.Data;
using Dapper;
using ParsingSDK.Parsing;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;

public static class NpgSqlTicketsStorageConverting
{
    extension(NpgSqlRegisteredTicketRow row)
    {
        public RegisterParserServiceTicket ToModel() => RegisterParserServiceTicket.From
        (
            row,
            idMap: d => d.Id,
            typeMap: d => d.Type,
            payloadMap: d => d.Payload,
            createdMap: d => d.Created,
            finishedMap: d => d.Finished,
            d => d.WasSent
        );
    }

    extension(IEnumerable<NpgSqlRegisteredTicketRow> rows)
    {
        public IEnumerable<RegisterParserServiceTicket> ToModels() =>
            rows.Select(row => row.ToModel());
    }

    extension(NpgSqlRegisteredTicketRow? row)
    {
        public Maybe<RegisterParserServiceTicket> MaybeTicket() => row == null 
            ? Maybe<RegisterParserServiceTicket>.None() 
            : Maybe<RegisterParserServiceTicket>.Some(row.ToModel());
    }

    extension(RegisterParserServiceTicket ticket)
    {
        public object ExtractParameters() => new
        {
            id = ticket.Id,
            type = ticket.Type,
            payload = ticket.Payload,
            created = ticket.Created,
            was_sent = ticket.WasSent,
            finished = ticket.Finished
        };
    }
    
    extension(QueryRegisteredTicketArgs args)
    {
        public (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();
        
            if (args.Id.HasValue)
            {
                filters.Add("id=@id");
                parameters.Add("@id", args.Id.Value, DbType.Guid);
            }

            if (args.FinishedOnly) filters.Add("finished is not NULL");
            if (args.NotFinishedOnly) filters.Add("finished is null");
            if (args.NotSentOnly) filters.Add("was_sent is false");
            if (args.SentOnly) filters.Add("was_sent IS TRUE");
        
            string filtersResult = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
            return (parameters, filtersResult);
        }

        public string LimitClause() => args.Limit.HasValue ? $"LIMIT {args.Limit}" : string.Empty;
    
        public string LockClause() => args.WithLock ? "FOR UPDATE" : string.Empty;
    }
}