using System.Data;
using Dapper;
using ParsingSDK.Parsing;

namespace RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

public interface IPostgreSqlAdapter : IDisposable, IAsyncDisposable
{
    Task<int> ExecuteCommand(CommandDefinition command, CancellationToken ct = default);
    Task ExecuteBulk(string sql, IEnumerable<object> parameters);

    CommandDefinition CreateCommand(
        string sql,
        Func<object>? parametersSource = null,
        CancellationToken ct = default);

    Task<IDataReader> GetRowsReader(CommandDefinition command, CancellationToken ct = default);

    CommandDefinition CreateCommand(
        string sql,
        object parameters,
        CancellationToken ct = default
    );

    CommandDefinition CreateCommand<T>(
        string sql,
        T? parametersSource = null,
        Func<T, object>? parametersFactory = null,
        CancellationToken ct = default) where T : class;

    CommandDefinition CreateCommand(
        string sql,
        DynamicParameters parameters,
        CancellationToken ct = default
    );

    Task<Maybe<T>> QuerySingle<T>(CommandDefinition command, CancellationToken ct = default) where T : class;

    Task<IEnumerable<T>> QueryMany<T>(CommandDefinition command, CancellationToken ct = default)
        where T : class;

    Task UseTransaction(CancellationToken ct = default);

    Task CommitTransaction(CancellationToken ct = default);
}