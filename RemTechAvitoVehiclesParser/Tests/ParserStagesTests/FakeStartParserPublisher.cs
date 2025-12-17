using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.ParserStagesTests;

public sealed class FakeStartParserPublisher(RabbitMqConnectionSource rabbitMq, Serilog.ILogger logger)
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<FakeStartParserPublisher>();
    public async Task Publish(FakeParserPublishPayload payload)
    {
        CancellationToken ct = CancellationToken.None;
        IConnection connection = await rabbitMq.GetConnection(ct);
        await using IChannel channel = await connection.CreateChannelAsync();

        string exchange = $"{payload.Parser.Domain}.{payload.Parser.Type}";
        string routingKey = $"{exchange}.start";

        ReadOnlyMemory<byte> body = payload.GetPayloadForRabbitMq();
        
        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            body: body,
            cancellationToken: ct
        );
        
        Logger.Information("Published message to {Exchange} with routing key {RoutingKey}", exchange, routingKey);
    }
}