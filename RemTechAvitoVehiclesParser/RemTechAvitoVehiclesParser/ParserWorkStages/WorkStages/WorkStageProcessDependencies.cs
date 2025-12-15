using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace RemTechAvitoVehiclesParser.ParserWorkStages.WorkStages;

public sealed record WorkStageProcessDependencies(
    BrowserFactory Browsers,
    AvitoBypassFactory Bypasses,
    TextTransformerBuilder TextTransformerBuilder,
    Serilog.ILogger Logger,
    NpgSqlConnectionFactory NpgSql
);
