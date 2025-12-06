using System.Data;
using Dapper;
using Npgsql;
using ParsingSDK.Parsing;
using RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

namespace ParsingSDK.Infrastructure.PostgreSql;

public sealed class NpgSqlSession(NpgSqlDataSourceFactory factory) : IPostgreSqlAdapter
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public async Task<int> ExecuteCommand(CommandDefinition command, CancellationToken ct = default)
    {
        IDbConnection connection = await GetConnection(ct);
        return await connection.ExecuteAsync(command);
    }

    public async Task ExecuteBulk(string sql, IEnumerable<object> parameters)
    {
        IDbConnection connection = await GetConnection(CancellationToken.None);
        await connection.ExecuteAsync(sql, parameters, transaction: _transaction);
    }
    
    public CommandDefinition CreateCommand(
        string sql, 
        Func<object>? parametersSource = null,
        CancellationToken ct = default)
    {
        object? parameters = parametersSource is null ? null : parametersSource();
        CommandDefinition command = new(sql, parameters, transaction: _transaction, cancellationToken: ct);
        return command;
    }

    public async Task<IDataReader> GetRowsReader(CommandDefinition command, CancellationToken ct = default)
    {
        IDbConnection connection = await GetConnection(ct);
        return await connection.ExecuteReaderAsync(command);
    }
    
    public CommandDefinition CreateCommand(
        string sql,
        object parameters,
        CancellationToken ct = default
        )
    {
        CommandDefinition command = new(sql, parameters, transaction: _transaction, cancellationToken: ct);
        return command;
    }
    
    public CommandDefinition CreateCommand<T>(
        string sql,
        T? parametersSource = null,
        Func<T, object>? parametersFactory = null,
        CancellationToken ct = default) where T : class
    {
        object? parameters = CreateParameters(parametersSource, parametersFactory);
        CommandDefinition command = new(sql, parameters, transaction: _transaction, cancellationToken: ct);
        return command;
    }

    public CommandDefinition CreateCommand(
        string sql,
        DynamicParameters parameters,
        CancellationToken ct = default
        )
    {
        CommandDefinition command = new(sql, parameters, transaction: _transaction, cancellationToken: ct);
        return command;
    }
    
    public async Task<Maybe<T>> QuerySingle<T>(CommandDefinition command, CancellationToken ct = default) where T : class
    {
        IDbConnection connection = await GetConnection(ct);
        T? element = await connection.QueryFirstOrDefaultAsync<T>(command);
        return Maybe<T>.Resolve(element);
    }

    public async Task<IEnumerable<T>> QueryMany<T>(CommandDefinition command, CancellationToken ct = default)
        where T : class
    {
        IDbConnection connection = await GetConnection(ct);
        IEnumerable<T> elements = await connection.QueryAsync<T>(command);
        return elements;
    }
    
    public async Task UseTransaction(CancellationToken ct = default)
    {
        NpgsqlConnection connection = await GetConnection(ct);
        _transaction ??= await connection.BeginTransactionAsync(ct);
    }

    public async Task CommitTransaction(CancellationToken ct = default)
    {
        if (_transaction == null) return;
        try
        {
            await _transaction.CommitAsync(ct);
        }
        catch(Exception ex)
        {
            await _transaction.RollbackAsync(ct);
            throw new ApplicationException("Unable to commit transaction", ex);
        }
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null) await _transaction.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }

    private object? CreateParameters<T>(
        T? parametersSource,
        Func<T, object>? parametersFactory
        ) where T : class
    {
        return (parametersSource, parametersFactory) switch
        {
            (not null, null) => throw new InvalidOperationException(
                "Cannot create parameters, when parametersSource is not null and parameters factory is null"),
            (null, not null) => throw new InvalidOperationException(
                "Cannot create parameters, when parametersSource is null and parameters factory is not null"),
            (not null, not null) => parametersFactory(parametersSource),
            (null, null) => null,
        };
    }
    
    private async Task<NpgsqlConnection> GetConnection(CancellationToken ct)
    {
        return _connection ??= await factory.GetConnection(ct);
    }
}