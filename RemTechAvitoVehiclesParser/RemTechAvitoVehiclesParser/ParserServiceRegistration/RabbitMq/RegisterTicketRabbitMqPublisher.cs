using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Models;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.RabbitMq;

public sealed class RegisterTicketRabbitMqPublisher(RabbitMqConnectionSource connectionFactory)
{
    private const string Queue = ConstantsForMainApplicationCommunication.CreateParserExchange;
    private const string Exchange = ConstantsForMainApplicationCommunication.CreateParserRoutingKey;
    private const string Type = "topic";
    private const string RoutingKey = ConstantsForMainApplicationCommunication.CreateParserRoutingKey;
    
    public async Task Publish(RegisterParserServiceTicket ticket, CancellationToken ct = default)
    {
        object normalizedPayload = CreateNormalizedPayload(ticket);
        string jsonPayload = CreateJsonPayload(normalizedPayload);
        ReadOnlyMemory<byte> bytePayload = CreateByteArrayPayload(jsonPayload);
        await PublishMessage(connectionFactory, bytePayload, ct);
    }
    
    private static object CreateNormalizedPayload(RegisterParserServiceTicket snapshot)
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

    private static async Task PublishMessage(RabbitMqConnectionSource factory, ReadOnlyMemory<byte> payload, CancellationToken ct)
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
        
        BasicProperties properties = new() { Persistent = true };
        
        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            basicProperties: properties,
            mandatory: true,
            body: payload,
            cancellationToken: ct);
    }
}