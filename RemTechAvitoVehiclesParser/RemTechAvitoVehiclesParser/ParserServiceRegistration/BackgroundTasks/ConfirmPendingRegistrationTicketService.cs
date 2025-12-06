using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

namespace RemTechAvitoVehiclesParser.ParserServiceRegistration.BackgroundTasks;

public sealed class ConfirmPendingRegistrationTicketService(
    IServiceProvider sp,
    Serilog.ILogger logger,
    RabbitMqConnectionSource rabbitMqConnectionFactory
    ) : BackgroundService
{
    private const string Queue = ConstantsForMainApplicationCommunication.CurrentServiceType;
    private const string Exchange = ConstantsForMainApplicationCommunication.CurrentServiceType;
    private const string Type = "topic";
    private const string RoutingKey = ConstantsForMainApplicationCommunication.CurrentServiceDomain;
    private readonly Serilog.ILogger _logger = logger.ForContext<ConfirmPendingRegistrationTicketService>();
    private IChannel _channel = null!;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CancellationToken ct = stoppingToken;
        IConnection connection = await rabbitMqConnectionFactory.GetConnection(ct);
        _channel = await connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: Type,
            autoDelete: false,
            durable: true,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: ct);

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += Handler();

        await _channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler() => async (_, @event) =>
    {
        _logger.Information("Received parser registration confirmation message");
        try
        {
            await using AsyncServiceScope scope = sp.CreateAsyncScope();
            string jsonPayload = Encoding.UTF8.GetString(@event.Body.ToArray());
            using JsonDocument document = JsonDocument.Parse(jsonPayload);
            Guid id = document.RootElement.GetProperty("ticket_id").GetGuid();
            ConfirmPendingCreationTicketCommand command = new(id);
            IConfirmPendingCreationTicket confirm = scope.ServiceProvider.GetRequiredService<IConfirmPendingCreationTicket>();
            await confirm.Handle(command);
        }
        catch(Exception ex)
        {
            _logger.Error(ex, "Error processing parser registration confirmation.");
        }
        finally
        {
            await _channel.BasicAckAsync(@event.DeliveryTag, false);
        }
    };
}