using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;
using RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.RabbitMq;

public sealed class RegisterTicketRabbitMqPublisher(RabbitMqConnectionFactory connectionFactory)
{
    private const string Queue = ConstantsForMainApplicationCommunication.CreateParserExchange;
    private const string Exchange = ConstantsForMainApplicationCommunication.CreateParserRoutingKey;
    private const string Type = "topic";
    private const string RoutingKey = ConstantsForMainApplicationCommunication.CreateParserRoutingKey;
    
    public async Task Publish<T>(
        ISnapshotSource<T, RegisterParserServiceTicketSnapshot> snapshotSource,
        CancellationToken ct = default) where T : class
    {
        RegisterParserServiceTicketSnapshot snapshot = snapshotSource.GetSnapshot();
        object normalizedPayload = CreateNormalizedPayload(snapshot);
        string jsonPayload = CreateJsonPayload(normalizedPayload);
        ReadOnlyMemory<byte> bytePayload = CreateByteArrayPayload(jsonPayload);
        await PublishMessage(connectionFactory, bytePayload, ct);
    }
    
    private static object CreateNormalizedPayload(RegisterParserServiceTicketSnapshot snapshot)
    {
        return new
        {
            ticket_id = snapshot.Id,
            parser_domain = ConstantsForMainApplicationCommunication.CurrentServiceDomain,
            parser_type = ConstantsForMainApplicationCommunication.CurrentServiceType,
        };
    }

    private static string CreateJsonPayload(object payload)
    {
        return JsonSerializer.Serialize(payload);
    }
    
    private static ReadOnlyMemory<byte> CreateByteArrayPayload(string json)
    {
        return Encoding.UTF8.GetBytes(json);
    }

    private static async Task PublishMessage(RabbitMqConnectionFactory factory, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        
        IConnection connection = await factory.GetConnection(ct);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: ct, options: options);

        await channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: Type,
            durable: true,
            autoDelete: false,
            cancellationToken: ct
        );

        await channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: ct
        );
        
        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            body: payload,
            cancellationToken: ct);
    }
}