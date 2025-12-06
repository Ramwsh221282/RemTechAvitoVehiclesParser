using Microsoft.Extensions.Options;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

public sealed class RemTechAvitoParserDbUpgrader : AbstractDatabaseUpgrader
{
    public RemTechAvitoParserDbUpgrader(IOptions<NpgSqlOptions> options) : base(options)
    {
        OfAssembly(typeof(RemTechAvitoParserDbUpgrader).Assembly);
    }
}