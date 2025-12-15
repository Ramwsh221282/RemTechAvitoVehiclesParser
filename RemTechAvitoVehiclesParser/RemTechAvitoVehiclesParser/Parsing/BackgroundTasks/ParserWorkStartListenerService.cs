using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages.Features.SaveEvaluationParserWorkStage;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

namespace RemTechAvitoVehiclesParser.Parsing.BackgroundTasks;

public sealed class ParserWorkStartListenerService(Serilog.ILogger logger, IServiceProvider sp, RabbitMqConnectionSource rabbitMq
    )
    : BackgroundService
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ParserWorkStartListenerService>();
    private const string Queue = ConstantsForMainApplicationCommunication.ParsersQueue;
    private const string Exchange = ConstantsForMainApplicationCommunication.ParsersExchange;
    private const string Type = "topic";
    private static readonly string RoutingKey = $"start.{ConstantsForMainApplicationCommunication.CurrentServiceDomain}.{ConstantsForMainApplicationCommunication.CurrentServiceType}";
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
            cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: Type,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

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
                string json = Encoding.UTF8.GetString(@event.Body.ToArray());
                using JsonDocument document = JsonDocument.Parse(json);

                Guid id = document.RootElement.GetProperty("parser_id").GetGuid();
                string domain = document.RootElement.GetProperty("parser_domain").GetString()!;
                string type = document.RootElement.GetProperty("parser_type").GetString()!;
                List<SaveEvaluationParserWorkLinkArg> links = [];
                foreach (JsonElement link in document.RootElement.GetProperty("parser_links").EnumerateArray())
                {
                    Guid linkId = link.GetProperty("id").GetGuid();
                    string linkUrl = link.GetProperty("url").GetString()!;
                    links.Add(new SaveEvaluationParserWorkLinkArg(linkId, linkUrl));
                }

                await using AsyncServiceScope scope = sp.CreateAsyncScope();
                SaveEvaluationParserWorkStageCommand command = new(id, domain, type, links);
                ISaveEvaluationParserWorkStage saveEvaluationWorkStage =
                    scope.ServiceProvider.GetRequiredService<ISaveEvaluationParserWorkStage>();
                await saveEvaluationWorkStage.Handle(command);

                _logger.Information("{Id} saved parser evaluation work stage.", id);
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