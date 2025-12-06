using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;
using RemTechAvitoVehiclesParser.SharedDependencies.RabbitMq;

namespace Tests.ParserServiceRegistrationTests;

public sealed class PublisherToParserRegistrationTicketApproval(RabbitMqConnectionFactory connectionFactory)
{
    private const string Queue = ConstantsForMainApplicationCommunication.CurrentServiceType;
    private const string Exchange = ConstantsForMainApplicationCommunication.CurrentServiceType;
    private const string Type = "topic";
    private const string RoutingKey = ConstantsForMainApplicationCommunication.CurrentServiceDomain;
    
    public async Task Publish(Guid ticketId, string domain, string type)
    {
        IConnection connection = await connectionFactory.GetConnection();
        IChannel channel = await connection.CreateChannelAsync();

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
            ticket_id = ticketId,
            parser_domain = domain,
            parser_type = type,
        };
        
        string jsonPayload = JsonSerializer.Serialize(payload);
        ReadOnlyMemory<byte> bytesPayload = Encoding.UTF8.GetBytes(jsonPayload);

        BasicProperties properties = new() { Persistent = true };
        await channel.BasicPublishAsync(
            exchange: Exchange,
            routingKey: RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: bytesPayload
            );
    }
}