namespace RemTechAvitoVehiclesParser.OnStartup.RegisterParser;

public interface IRegisterParserOnStartup
{
    Task Invoke(string domain, string type);
}