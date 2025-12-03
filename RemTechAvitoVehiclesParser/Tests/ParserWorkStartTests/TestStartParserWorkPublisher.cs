using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;

namespace Tests.ParserWorkStartTests;

public sealed class TestStartParserWorkPublisher(
    RabbitMqConnectionFactory connectionFactory
    )
{
    private const string Queue = ConstantsForMainApplicationCommunication.ParsersQueue;
    private const string Exchange = ConstantsForMainApplicationCommunication.ParsersExchange;
    private const string Type = "topic";
    private static readonly string RoutingKey = $"start.{ConstantsForMainApplicationCommunication.CurrentServiceDomain}.{ConstantsForMainApplicationCommunication.CurrentServiceType}";
    
    public async Task Publish(Guid id, string domain, string type, IEnumerable<(Guid, string)> links)
    {
        IConnection connection = await connectionFactory.GetConnection();
        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        IChannel channel = await connection.CreateChannelAsync(options);

        await channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: Type,
            durable: true,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey);

        object payload = new
        {
            parser_id = id,
            parser_domain = domain,
            parser_type = type,
            parser_links = links.Select(l => new
            {
                id = l.Item1,
                url = l.Item2,
            })
        };

        string json = JsonSerializer.Serialize(payload);
        ReadOnlyMemory<byte> bytes = Encoding.UTF8.GetBytes(json);

        BasicProperties properties = new() { Persistent = true };
        
        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: bytes
        );
    }
}