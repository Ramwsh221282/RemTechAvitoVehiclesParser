using Microsoft.Extensions.Options;
using Npgsql;

namespace RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

public sealed class NpgSqlDataSourceFactory
{
    private readonly Lazy<NpgsqlDataSource> _lazyDataSource;

    public NpgSqlDataSourceFactory(IOptions<NpgSqlOptions> options)
    {
        _lazyDataSource = new Lazy<NpgsqlDataSource>(() =>
        {
            NpgsqlDataSourceBuilder builder = new NpgsqlDataSourceBuilder(options.Value.ToConnectionString());
            NpgsqlDataSource dataSource = builder.Build();
            return dataSource;
        });
    }
    
    public async Task<NpgsqlConnection> CreateConnection(CancellationToken ct = default)
    {
        return await _lazyDataSource.Value.OpenConnectionAsync(ct);
    }
}