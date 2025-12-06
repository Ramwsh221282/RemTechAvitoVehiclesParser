namespace RemTechAvitoVehiclesParser.SharedDependencies.PostgreSql;

public sealed class NpgSqlOptions
{
    private const string ConnectionStringTemplate =
        "Host={0};Port={1};Database={2};Username={3};Password={4};";
    public string Host { get; set; } = null!;
    public string Port { get; set; } = null!;
    public string Database { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ToConnectionString() =>
        string.Format(ConnectionStringTemplate, Host, Port, Database, Username, Password);
}