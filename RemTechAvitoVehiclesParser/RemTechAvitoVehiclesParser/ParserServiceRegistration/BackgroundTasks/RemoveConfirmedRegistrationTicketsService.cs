using Quartz;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Database;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;
using RemTechAvitoVehiclesParser.SharedDependencies.Quartz;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.BackgroundTasks;

[DisallowConcurrentExecution]
[CronSchedule("*/5 * * * * ?")]
public sealed class RemoveConfirmedRegistrationTicketsService(
    NpgSqlDataSourceFactory dataSourceFactory,
    Serilog.ILogger logger
    )
    : ICronScheduleJob
{
    private readonly Serilog.ILogger _logger = logger.ForContext<RemoveConfirmedRegistrationTicketsService>();
    
    public async Task Execute(IJobExecutionContext context)
    {
        await using IPostgreSqlAdapter session = await dataSourceFactory.CreateAdapter(context.CancellationToken);
        NpgSqlRegisteredTicketsStorage storage = new(session);
            
        await session.UseTransaction();
        _logger.Information("Executing removing confirmed registration tickets job.");
        CancellationToken ct = context.CancellationToken;
        QueryRegisteredTicketArgs args = new(FinishedOnly: true, SentOnly: true, Limit: 50, WithLock: true);
        RegisterParserServiceTicket[] tickets = [..await storage.GetTickets(args, ct)];
        if (tickets.Length == 0)
        {
            _logger.Information("No tickets found. Stopping removing confirmed registration tickets job.");
            return;
        }

        int removed = await storage.DeleteMany(tickets);
            
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