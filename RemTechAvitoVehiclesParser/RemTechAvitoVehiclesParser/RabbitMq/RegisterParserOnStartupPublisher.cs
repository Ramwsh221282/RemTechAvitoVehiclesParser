using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace RemTechAvitoVehiclesParser.RabbitMq;

public sealed class RegisterParserOnStartupPublisher(RabbitMqConnectionSource connectionSource)
{
    private const string Exchange = "";
    private const string RoutingKey = "";

    public async Task PublishParserRegistration(
        string parserDomain,
        string parserType, 
        CancellationToken ct = default)
    {
        IConnection connection = await connectionSource.GetConnection(ct);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            mandatory: false,
            body: CreateMessageBody(parserDomain, parserType), 
            cancellationToken: ct);
    }

    private ReadOnlyMemory<byte> CreateMessageBody(string parserDomain, string parserType)
    {
        object message = new
        {
            parser_domain = parserDomain,
            parser_type = parserType
        };

        string json = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(json);
    }
}