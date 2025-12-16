namespace Tests.ParserServiceRegistrationTests;

public sealed class TestParserRegistrationTicketApprovalService(
    RabbitMqConnectionSource rabbitMqConnectionFactory,
    Serilog.ILogger logger
    ) : BackgroundService
{
    public static readonly List<string> Messages = [];
    private const string Queue = ConstantsForMainApplicationCommunication.CreateParserExchange;
    private const string Exchange = ConstantsForMainApplicationCommunication.CreateParserRoutingKey;
    private const string Type = "topic";
    private const string RoutingKey = ConstantsForMainApplicationCommunication.CreateParserRoutingKey;

    private IChannel _channel = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await rabbitMqConnectionFactory.GetConnection(stoppingToken);
        _channel = await connection.CreateChannelAsync();

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

        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += Handler;

        await _channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
            );
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler => async (sender, @event) =>
    {
        logger.Information("Message recieved.");
        string json = Encoding.UTF8.GetString(@event.Body.Span);

        logger.Information("""
                           Message information:
                           {Json}
                           """, json);

        Messages.Add(json);
        await _channel.BasicAckAsync(@event.DeliveryTag, false);
    };
}