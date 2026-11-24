using Microsoft.Extensions.Configuration;

namespace SalesMcp.Server.Stdio;

/// <summary>Settings read from configuration at start-up.</summary>
/// <param name="DatabasePath">Absolute path of the SQLite database file.</param>
public sealed record ServerSettings(string DatabasePath)
{
    /// <summary>The configuration key for the database path.</summary>
    public const string DatabasePathKey = "Database:Path";

    /// <summary>
    /// Reads the settings. The database defaults to a per-user data folder, because MCP hosts launch
    /// the server from directories we can't predict, so a relative default would scatter databases.
    /// </summary>
    public static ServerSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration[DatabasePathKey];
        var path = string.IsNullOrWhiteSpace(configured) ? DefaultDatabasePath() : Path.GetFullPath(configured);
        return new ServerSettings(path);
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
