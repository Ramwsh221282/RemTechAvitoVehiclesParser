using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RemTechAvitoVehiclesParser.Configuration;

namespace RemTechAvitoVehiclesParser.RabbitMq;

public sealed class RabbitMqConnectionSource(IOptions<RabbitMqConnectionOptions> options)
{
    private IConnection? _connection;
    
    public async Task<IConnection> GetConnection(CancellationToken ct = default)
    {
        return _connection ??= await new ConnectionFactory()
        {
            HostName = options.Value.Hostname,
            UserName = options.Value.Username,
            Password = options.Value.Password,
            Port = options.Value.Port,
        }.CreateConnectionAsync(ct);
    }
}