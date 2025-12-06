namespace RemTechAvitoVehiclesParser.SharedDependencies.Constants;

public static class ConstantsForMainApplicationCommunication
{
    public const string CurrentServiceDomain = "Avito";
    public const string CurrentServiceType = "Техника";
    public const string CreateParserExchange = "parsers";
    public const string CreateParserRoutingKey = "parsers.creation";
    public const string ParsersQueue = "parsers";
    public const string ParsersExchange = "parsers";
}