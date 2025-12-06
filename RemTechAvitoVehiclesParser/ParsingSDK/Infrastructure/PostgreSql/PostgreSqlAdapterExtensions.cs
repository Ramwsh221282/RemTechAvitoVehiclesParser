namespace RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

public static class PostgreSqlAdapterExtensions
{
    extension(IPostgreSqlAdapter adapter)
    {
        public async Task<bool> TransactionCommited(CancellationToken ct = default)
        {
            try
            {
                await adapter.CommitTransaction(ct);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}