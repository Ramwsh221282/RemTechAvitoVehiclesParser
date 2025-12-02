using Microsoft.Extensions.Options;
using Npgsql;
using RemTechAvitoVehiclesParser.Configuration;

namespace RemTechAvitoVehiclesParser.Database;

public sealed class NpgSqlDataSourceContainer
{
    private readonly Lazy<NpgsqlDataSource> _lazyDataSource;

    public NpgSqlDataSourceContainer(IOptions<NpgSqlOptions> options)
    {
        _lazyDataSource = new Lazy<NpgsqlDataSource>(() =>
        {
            NpgsqlDataSourceBuilder builder = new NpgsqlDataSourceBuilder(options.Value.ToConnectionString());
            NpgsqlDataSource dataSource = builder.Build();
            return dataSource;
        });
    }
    
    public async Task<NpgsqlConnection> GetConnection(CancellationToken ct = default)
    {
        return await _lazyDataSource.Value.OpenConnectionAsync(ct);
    }
}