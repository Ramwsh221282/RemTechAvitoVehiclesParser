using Quartz;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class RemoveConfirmedRegistrationTicketsService(
    NpgSqlSession session,
    Serilog.ILogger logger
    )
    : ICronScheduleJob
{
    private readonly NpgSqlRegisteredTicketsStorage _storage = new(session);
    private readonly Serilog.ILogger _logger = logger.ForContext<RemoveConfirmedRegistrationTicketsService>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        await using (session)
        {
            await session.UseTransaction();
            _logger.Information("Executing removing confirmed registration tickets job.");
            CancellationToken ct = context.CancellationToken;
            QueryRegisteredTicketArgs args = new(FinishedOnly: true, SentOnly: true, Limit: 50);
            RegisterParserServiceTicket[] tickets = [..await _storage.GetTickets(args, ct)];
            if (tickets.Length == 0)
            {
                _logger.Information("No tickets found. Stopping removing confirmed registration tickets job.");
                return;
            }

            int removed = await _storage.DeleteMany(tickets);
            
            try
            {
                await session.CommitTransaction();
                _logger.Information("Removed {Count} confirmed registration tickets.", removed);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error at committing transaction in removing confirmed registration tickets job.");
            }
        }
    }
}