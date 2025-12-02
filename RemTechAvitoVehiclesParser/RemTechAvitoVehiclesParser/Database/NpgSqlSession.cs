using System.Data;
using Npgsql;

namespace RemTechAvitoVehiclesParser.Database;

public sealed class NpgSqlSession(NpgSqlDataSourceContainer container) : IDisposable, IAsyncDisposable
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    public IDbTransaction? Transaction => _transaction;
    
    public async Task Execute(Func<IDbConnection, Task> fn, CancellationToken ct = default)
    {
        _connection ??= await container.GetConnection(ct);
        await fn(_connection);
    }

    public async Task<U> Execute<U>(Func<IDbConnection, Task<U>> fn, CancellationToken ct = default)
    {
        _connection ??= await container.GetConnection(ct);
        return await fn(_connection);
    }

    public async Task MakeTransactional(CancellationToken ct = default)
    {
        NpgsqlConnection connection = await container.GetConnection(ct);
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
}