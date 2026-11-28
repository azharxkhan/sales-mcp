using Microsoft.Extensions.Configuration;

namespace SalesMcp.Server.Stdio;

/// <summary>Settings read from configuration at start-up.</summary>
/// <param name="DatabasePath">Absolute path of the SQLite database file.</param>
/// <param name="CustomerAccount">The account number of the customer to bind the session to, if given.</param>
/// <param name="Agent">The id of the agent serving the session, recorded on quotes.</param>
public sealed record ServerSettings(string DatabasePath, string? CustomerAccount, string Agent = ServerSettings.DefaultAgent)
{
    /// <summary>The agent recorded on quotes when none is configured: the demo business's agent.</summary>
    public const string DefaultAgent = "acme-distributor-jolt";

    /// <summary>
    /// The configuration key for the session's agent: <c>--agent</c> on the command line or
    /// <c>SALESMCP_AGENT</c> in the environment. Temporary until agent profiles bind it in M3.
    /// </summary>
    public const string AgentKey = "Agent";

    /// <summary>The configuration key for the database path.</summary>
    public const string DatabasePathKey = "Database:Path";

    /// <summary>
    /// The configuration key for the session's customer: <c>--customer</c> on the command line or
    /// <c>SALESMCP_CUSTOMER</c> in the environment. Temporary until agent profiles bind it in M3.
    /// </summary>
    public const string CustomerKey = "Customer";

    /// <summary>
    /// Reads the settings. The database defaults to a per-user data folder, because MCP hosts launch
    /// the server from directories we can't predict, so a relative default would scatter databases.
    /// </summary>
    public static ServerSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration[DatabasePathKey];
        var path = string.IsNullOrWhiteSpace(configured) ? DefaultDatabasePath() : Path.GetFullPath(configured);
        var customer = configuration[CustomerKey];
        var agent = configuration[AgentKey];
        return new ServerSettings(
            path,
            string.IsNullOrWhiteSpace(customer) ? null : customer.Trim(),
            string.IsNullOrWhiteSpace(agent) ? DefaultAgent : agent.Trim());
    }

    /// <summary>The SQLite connection string for <see cref="DatabasePath"/>.</summary>
    public string ConnectionString => $"Data Source={DatabasePath}";

    /// <summary><c>%LOCALAPPDATA%\sales-mcp\sales.db</c> on Windows, <c>~/.local/share/sales-mcp/sales.db</c> on Linux.</summary>
    public static string DefaultDatabasePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            "sales-mcp",
            "sales.db");
}
