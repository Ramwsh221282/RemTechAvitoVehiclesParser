using Quartz;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.RabbitMq;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class PublishPendingRegistrationTicketsToQueue(
    Serilog.ILogger logger,
    NpgSqlDataSourceFactory dataSourceFactory,
    RabbitMqConnectionFactory factory
    ) : ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<PublishPendingRegistrationTicketsToQueue>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.Information("Starting publishing pending registration messages.");
        CancellationToken ct = context.CancellationToken;

        await using IPostgreSqlAdapter session = await dataSourceFactory.CreateAdapter(context.CancellationToken);
        RegisterTicketRabbitMqPublisher publisher = new(factory);
        NpgSqlRegisteredTicketsStorage storage = new(session);
            
        await session.UseTransaction(ct: ct);

        QueryRegisteredTicketArgs args = new(NotSentOnly: true, Limit: 50, WithLock: true, NotFinishedOnly: true);
        RegisterParserServiceTicket[] pendingTickets = [..await storage.GetTickets(args, ct)];
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
                await publisher.Publish(pending, ct: ct);
                RegisterParserServiceTicket sent = pending.MarkSent();
                succeeded.Add(sent);
                LogSuccessPublishing(sent.GetSnapshot());
            }
            catch(Exception ex)
            {
                LogFailurePublishing(ex);
            }
        }

        await storage.UpdateMany(succeeded, ct);
        
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
        object[] logProperties = [snapshot.Id, snapshot.Type, snapshot.Payload, snapshot.WasSent];
        _logger.Information(
            """
            Published ticket info:
            Id: {Id}
            Type: {Type}
            Payload: {Payload}
            Was sent: {WasSent}
            """, logProperties);
    }

    private void LogFailurePublishing(Exception ex)
    {
        _logger.Information(ex, "Published ticket error");
    }
}