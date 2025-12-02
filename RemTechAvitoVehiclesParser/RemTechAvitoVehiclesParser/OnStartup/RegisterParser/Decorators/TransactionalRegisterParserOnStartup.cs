using RemTechAvitoVehiclesParser.Database;

namespace RemTechAvitoVehiclesParser.OnStartup.RegisterParser.Decorators;

public sealed class TransactionalRegisterParserOnStartup(
    NpgSqlSession session,
    IRegisterParserOnStartup origin
    )
    : IRegisterParserOnStartup
{
    private readonly IRegisterParserOnStartup _origin = origin;
    private readonly NpgSqlSession _session = session;

    public async Task Invoke(string domain, string type)
    {
        await using (_session)
        {
            await _origin.Invoke(domain, type);
            await _session.CommitTransaction();
        }
    }
}