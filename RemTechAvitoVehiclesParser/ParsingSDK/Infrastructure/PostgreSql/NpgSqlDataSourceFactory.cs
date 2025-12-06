using Microsoft.Extensions.Options;
using Npgsql;
using ParsingSDK.Infrastructure.PostgreSql;

namespace RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

public sealed class NpgSqlDataSourceFactory
{
    private readonly Lazy<NpgsqlDataSource> _lazyDataSource;
    private readonly IOptions<NpgSqlOptions> _options;

    public NpgSqlDataSourceFactory(IOptions<NpgSqlOptions> options)
    {
        _options = options;
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
    
    public async Task<IPostgreSqlAdapter> CreateAdapter(CancellationToken ct = default)
    {
        NpgsqlConnection connection = await _lazyDataSource.Value.OpenConnectionAsync(ct);
        ScopedNpgSqlSession session = new(connection);
        return session;
    }
}