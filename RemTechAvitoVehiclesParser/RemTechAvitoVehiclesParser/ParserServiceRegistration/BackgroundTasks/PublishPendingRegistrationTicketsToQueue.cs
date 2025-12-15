using Quartz;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.RabbitMq;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class PublishPendingRegistrationTicketsToQueue(
    Serilog.ILogger logger,
    NpgSqlConnectionFactory npgSql,
    RabbitMqConnectionSource rabbitMq
    ) :
    ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<PublishPendingRegistrationTicketsToQueue>();

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        await using NpgSqlSession session = new NpgSqlSession(npgSql);
        RegisterTicketRabbitMqPublisher publisher = new(rabbitMq);
        NpgSqlRegisteredTicketsStorage storage = new(session);
        await session.UseTransaction(ct: ct);

        QueryRegisteredTicketArgs args = new(NotSentOnly: true, Limit: 50, WithLock: true, NotFinishedOnly: true);
        RegisterParserServiceTicket[] pendingTickets = [.. await storage.GetTickets(args, ct)];
        if (pendingTickets.Length == 0) return;

        List<RegisterParserServiceTicket> succeeded = [];
        foreach (RegisterParserServiceTicket pending in pendingTickets)
        {
            try
            {
                await publisher.Publish(pending, ct: ct);
                RegisterParserServiceTicket sent = pending.MarkSent();
                succeeded.Add(sent);
                LogSuccessPublishing(sent);
            }
            catch (Exception ex)
            {
                LogFailurePublishing(ex);
            }
        }

        await storage.UpdateMany(succeeded);

        try
        {
            await session.UnsafeCommit(ct);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error at commiting updating tickets in database.");
        }
    }

    private void LogSuccessPublishing(RegisterParserServiceTicket ticket)
    {
        _logger.Information(
            """
            Published ticket info:
            Id: {Id}
            Type: {Type}
            Payload: {Payload}
            Was sent: {WasSent}
            """,
            ticket.Id,
            ticket.Type,
            ticket.Payload,
            ticket.WasSent);
    }

    private void LogFailurePublishing(Exception ex)
    {
        _logger.Information(ex, "Published ticket error");
    }
}