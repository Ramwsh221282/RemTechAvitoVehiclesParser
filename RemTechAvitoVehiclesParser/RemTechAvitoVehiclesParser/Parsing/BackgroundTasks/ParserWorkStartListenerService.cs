using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models;
using RemTechAvitoVehiclesParser.ParserWorkStages.CatalogueParsing.Models.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Extensions;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

namespace RemTechAvitoVehiclesParser.Parsing.BackgroundTasks;

public sealed class ParserWorkStartListenerService(
    Serilog.ILogger logger,
    RabbitMqConnectionSource rabbitMq,
    NpgSqlConnectionFactory npgSql
) : BackgroundService
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ParserWorkStartListenerService>();
    private const string Queue = ConstantsForMainApplicationCommunication.ParsersQueue;
    private const string Exchange = ConstantsForMainApplicationCommunication.ParsersExchange;
    private const string Type = "topic";
    private static readonly string RoutingKey =
        $"start.{ConstantsForMainApplicationCommunication.CurrentServiceDomain}.{ConstantsForMainApplicationCommunication.CurrentServiceType}";
    private IChannel _channel = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await rabbitMq.GetConnection(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: Type,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken
        );

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += Handler();

        await _channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler()
    {
        return async (_, @event) =>
        {
            _logger.Information("Received message to start scraping.");

            try
            {
                await using NpgSqlSession session = new(npgSql);
                await session.UseTransaction();
                if (await ProcessingParser.HasAny(session))
                {
                    _logger.Information("There is already a parser in progress. Declining.");
                    return;
                }

                ProcessingParser parser = ProcessingParser.FromDeliverEventArgs(@event);
                ProcessingParserLink[] links = ProcessingParserLink.FromDeliverEventArgs(@event);
                ParserWorkStage stage = ParserWorkStage.PaginationStage();
                await stage.Persist(session);
                await parser.Persist(session);
                await links.PersistMany(session);
                await session.UnsafeCommit(CancellationToken.None);

                _logger.Information(
                    """
                    Added parser to process:
                    Domain: {domain}
                    Type: {type}
                    Id: {id}
                    Stage: {Stage}
                    Links: {Count}
                    """,
                    parser.Domain,
                    parser.Type,
                    parser.Id,
                    stage.Name,
                    links.Length
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error at saving parser evaluation work stage");
            }
            finally
            {
                await _channel.BasicAckAsync(@event.DeliveryTag, false);
            }
        };
    }
}
