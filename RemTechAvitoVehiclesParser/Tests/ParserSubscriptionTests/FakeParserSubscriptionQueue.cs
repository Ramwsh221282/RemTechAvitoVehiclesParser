using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.ParserSubscriptionTests;

public sealed class FakeParserSubscriptionQueue(
    RabbitMqConnectionSource rabbitMq,
    Serilog.ILogger logger,
    FakeParserSubscriptionQueuePublisher publisher) : BackgroundService
{
    private RabbitMqConnectionSource ConnectionSource { get; } = rabbitMq;
    private Serilog.ILogger Logger { get; } = logger;
    private FakeParserSubscriptionQueuePublisher Publisher { get; } = publisher;
    private IChannel Channel { get; set; } = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await ConnectionSource.GetConnection(stoppingToken);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
            );
        
        Channel = await connection.CreateChannelAsync(options);

        await Channel.ExchangeDeclareAsync(
            exchange: "parsers",
            type: "topic",
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
            );

        await Channel.QueueDeclareAsync(
            queue: "parsers.create",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
            );

        await Channel.QueueBindAsync(
            queue: "parsers.create",
            exchange: "parsers",
            routingKey: "parsers.create",
            cancellationToken: stoppingToken
            );
        
        AsyncEventingBasicConsumer consumer = new(Channel);
        consumer.ReceivedAsync += Handler;
        
        await Channel.BasicConsumeAsync(
            queue: "parsers.create",
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
            );
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler => async (_, @event) =>
    {
        Logger.Information("Received message to register parser.");
        byte[] body = @event.Body.ToArray();
        string json = Encoding.UTF8.GetString(body);
        using JsonDocument document = JsonDocument.Parse(json);

        Guid parserId = document.RootElement.GetProperty("parser_id").GetGuid();
        string parserType = document.RootElement.GetProperty("parser_type").GetString()!;
        string parserDomain = document.RootElement.GetProperty("parser_domain").GetString()!;
        
        await Publisher.Publish(parserId, parserType, parserDomain);
        Logger.Information($"Published parser registration message: {parserId}, {parserType}, {parserDomain}");
    };
}