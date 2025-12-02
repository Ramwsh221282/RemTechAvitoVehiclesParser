using Quartz;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.RabbitMq;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.BackgroundTasks;

[CronSchedule("*/10 * * * * ?")]
public sealed class PublishPendingRegistrationTicketsToQueue(
    NpgSqlSession session,
    Serilog.ILogger logger,
    RabbitMqConnectionFactory factory
    ) : ICronScheduleJob
{
    private readonly RegisterTicketRabbitMqPublisher _publisher = new(factory);
    private readonly NpgSqlRegisteredTicketsStorage _storage = new(session);
    private readonly Serilog.ILogger _logger = logger.ForContext<PublishPendingRegistrationTicketsToQueue>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.Information("Starting publishing pending registration messages.");
        CancellationToken ct = context.CancellationToken;
        await session.UseTransaction(ct: ct);

        QueryRegisteredTicketArgs args = new(NotSentOnly: true, Limit: 50, WithLock: true);
        RegisterParserServiceTicket[] pendingTickets = [..await _storage.GetTickets(args, ct)];
        if (pendingTickets.Length == 0)
        {
            _logger.Information("Stopping publishing pending registration messages. No pending messages.");
            return;
        }

        List<RegisterParserServiceTicket> succeeded = [];
        foreach (RegisterParserServiceTicket pending in pendingTickets)
        {
            try
            {
                await _publisher.Publish(pending, ct: ct);
                RegisterParserServiceTicket sent = pending.MarkSent();
                succeeded.Add(sent);
                LogSuccessPublishing(sent.GetSnapshot());
            }
            catch(Exception ex)
            {
                LogFailurePublishing(ex);
            }
        }

        await _storage.UpdateMany(succeeded, ct);
        
        try
        {
            await session.CommitTransaction(ct);
        }
        catch(Exception ex)
        {
            _logger.Error(ex, "Error at commiting updating tickets in database.");
        }
    }

    private void LogSuccessPublishing(RegisterParserServiceTicketSnapshot snapshot)
    {
        object[] logProperties = [snapshot.Id, snapshot.Type, snapshot.Payload];
        _logger.Information(
            """
            Published ticket info:
            Id: {Id}
            Type: {Type}
            Payload: {Payload}
            """, logProperties);
    }

    private void LogFailurePublishing(Exception ex)
    {
        _logger.Information(ex, "Published ticket error");
    }
}