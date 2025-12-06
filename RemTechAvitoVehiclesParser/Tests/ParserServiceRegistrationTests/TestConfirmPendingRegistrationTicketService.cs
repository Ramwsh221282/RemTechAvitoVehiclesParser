using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using RemTechAvitoVehiclesParser.ParserServiceRegistration.Features.ConfirmPendingCreationTicket;
using RemTechAvitoVehiclesParser.SharedDependencies.Constants;

namespace Tests.ParserServiceRegistrationTests;

public sealed class TestConfirmPendingRegistrationTicketService(
    IServiceProvider sp,
    Serilog.ILogger logger,
    RabbitMqConnectionSource rabbitMqConnectionFactory
    ) : BackgroundService
{
    private const string Queue = ConstantsForMainApplicationCommunication.CurrentServiceDomain;
    private const string Exchange = ConstantsForMainApplicationCommunication.CurrentServiceType;
    private const string Type = "topic";
    private static readonly string RoutingKey = 
        $"{ConstantsForMainApplicationCommunication.CurrentServiceDomain}{ConstantsForMainApplicationCommunication.CurrentServiceType}";
    private readonly Serilog.ILogger _logger = logger.ForContext<TestConfirmPendingRegistrationTicketService>();
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
            autoDelete: false,
            durable: true,
            cancellationToken: stoppingToken
            );

        await _channel.QueueBindAsync(
            queue: Queue,
            exchange: Exchange,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken
            );

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += Handler();

        await _channel.BasicConsumeAsync(
            queue: Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
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
            string? parserDomain = document.RootElement.GetProperty("parser_domain").GetString();
            string? parserType = document.RootElement.GetProperty("parser_type").GetString();
            ConfirmPendingCreationTicketCommand command = new(id);
            IConfirmPendingCreationTicket confirm = scope.ServiceProvider.GetRequiredService<IConfirmPendingCreationTicket>();
            await confirm.Handle(command);
            _logger.Information("Handled confirmation for {Id} {Domain} {Type}", id, parserDomain, parserType);
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