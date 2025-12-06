using System.Data;
using Dapper;
using ParsingSDK;
using ParsingSDK.Parsing;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;

// CREATE TABLE IF NOT EXISTS avito_parser_module.parser_tickets
// (
//     id uuid primary key,    
//     type varchar(256) not null,
//     payload jsonb not null,
//     created timestamptz not null,
//     was_sent boolean,
//     finished timestamptz
// );
public sealed class NpgSqlRegisteredTicketsStorage(IPostgreSqlAdapter session)
{
    private static Func<RegisterParserServiceTicketSnapshot, object> _parametersFactory = CreateParametersFromSnapshot;

    public async Task<Maybe<RegisterParserServiceTicket>> GetTicket(QueryRegisteredTicketArgs args, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = CreateFilters(args);
        string lockClause = CreateLockClause(args);
        string sql = $"""
                     SELECT id, type, payload, created, was_sent, finished
                     FROM avito_parser_module.parser_tickets
                     {filterSql}
                     {lockClause}
                     LIMIT 1
                     """;
        CommandDefinition command = session.CreateCommand(sql, parameters, ct: ct);
        Maybe<NpgSqlRegisteredTicketRow> row = await session.QuerySingle<NpgSqlRegisteredTicketRow>(command, ct);
        return row.HasValue
            ? Maybe<RegisterParserServiceTicket>.Some(RegisterParserServiceTicket.FromSnapshot(row.Value.GetSnapshot()))
            : Maybe<RegisterParserServiceTicket>.None();
    }

    public async Task<IEnumerable<RegisterParserServiceTicket>> GetTickets(
        QueryRegisteredTicketArgs args,
        CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = CreateFilters(args);
        string limitClause = CreateLimitClause(args);
        string lockClause = CreateLockClause(args);
        string sql = $"""
                      SELECT id, type, payload, created, was_sent, finished
                      FROM avito_parser_module.parser_tickets
                      {filterSql}
                      {lockClause}
                      {limitClause}
                      """;
        CommandDefinition command = session.CreateCommand(sql, parameters, ct: ct);
        IEnumerable<NpgSqlRegisteredTicketRow> rows = await session.QueryMany<NpgSqlRegisteredTicketRow>(command, ct);
        return rows.Select(r => RegisterParserServiceTicket.FromSnapshot(r.GetSnapshot()));
    }
    
    public async Task Update(RegisterParserServiceTicket ticket, CancellationToken ct = default)
    {
        const string sql = """
                           UPDATE avito_parser_module.parser_tickets
                           SET was_sent = @was_sent,
                               finished = @finished
                           WHERE id = @id
                           """;
        RegisterParserServiceTicketSnapshot snapshot = ticket.GetSnapshot();
        CommandDefinition command = session.CreateCommand(sql, snapshot, _parametersFactory, ct: ct);
        await session.ExecuteCommand(command, ct);
    }
    
    public async Task Store<T>(ISnapshotSource<T, RegisterParserServiceTicketSnapshot> snapshotSource, CancellationToken ct = default) where T : class
    {
        const string sql = """
                           INSERT INTO avito_parser_module.parser_tickets
                           (id, type, payload, created, was_sent, finished)
                           VALUES
                           (@id, @type, @payload::jsonb, @created, @was_sent, @finished)
                           """;
        RegisterParserServiceTicketSnapshot snapshot = snapshotSource.GetSnapshot();
        await session.ExecuteCommand(session.CreateCommand(sql, snapshot, _parametersFactory, ct));
    }

    public async Task<int> DeleteMany(IEnumerable<RegisterParserServiceTicket> tickets)
    {
        const string sql = """
                           DELETE FROM avito_parser_module.parser_tickets
                           WHERE id = ANY(@ids);
                           """;
        Guid[] identifiers = tickets.Select(t => t.GetSnapshot().Id).ToArray();
        CommandDefinition command = session.CreateCommand(sql, () => new { ids = identifiers });
        return await session.ExecuteCommand(command);
    }
    
    public async Task UpdateMany<T>(
        IEnumerable<ISnapshotSource<T, RegisterParserServiceTicketSnapshot>> snapshotSources,
        CancellationToken ct = default)
        where T : class
    {
        const string sql = """
                           UPDATE avito_parser_module.parser_tickets
                           SET was_sent = @was_sent,
                               finished = @finished
                           WHERE id = @id
                           """;
        IEnumerable<object> parameters = snapshotSources.Select(s => CreateParametersFromSnapshot(s.GetSnapshot()));
        await session.ExecuteBulk(sql, parameters);
    }

    private static (DynamicParameters parameters, string filterSql) CreateFilters(QueryRegisteredTicketArgs args)
    {
        List<string> filters = [];
        DynamicParameters parameters = new();
        
        if (args.Id.HasValue)
        {
            filters.Add("id=@id");
            parameters.Add("@id", args.Id.Value, DbType.Guid);
        }

        if (args.FinishedOnly)
        {
            filters.Add("finished is not NULL");
        }

        if (args.NotFinishedOnly)
        {
            filters.Add("finished is null");
        }

        if (args.NotSentOnly)
        {
            filters.Add("was_sent is false");
        }
        
        if (args.SentOnly)
        {
            filters.Add("was_sent IS TRUE");
        }
        
        string filtersResult = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        return (parameters, filtersResult);
    }

    private static string CreateLimitClause(QueryRegisteredTicketArgs args)
    {
        return args.Limit.HasValue ? $"LIMIT {args.Limit}" : string.Empty;
    }
    
    private static string CreateLockClause(QueryRegisteredTicketArgs args)
    {
        return args.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    private static object CreateParametersFromSnapshot(RegisterParserServiceTicketSnapshot snapshot)
    {
        return new
        {
            id = snapshot.Id,
            type = snapshot.Type,
            payload = snapshot.Payload,
            created = snapshot.Created,
            was_sent = snapshot.WasSent,
            finished = snapshot.Finished
        };
    }
}