using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using SalesMcp.Infrastructure;
using SalesMcp.Infrastructure.Persistence;

namespace SalesMcp.Server.Stdio;

/// <summary>
/// Composes the stdio MCP server. Kept apart from <c>Program</c> so the integration tests can build
/// the same composition the executable runs.
/// </summary>
public static class SalesMcpHost
{
    /// <summary>The name the server reports to MCP clients.</summary>
    public const string ServerName = "sales-mcp";

    /// <summary>The prefix for environment variables, for example <c>SALESMCP_Database__Path</c>.</summary>
    public const string EnvironmentPrefix = "SALESMCP_";

    private static readonly Dictionary<string, string> _switches = new(StringComparer.OrdinalIgnoreCase)
    {
        ["--database"] = ServerSettings.DatabasePathKey,
    };

    /// <summary>The server's version, from the assembly's informational version.</summary>
    public static string Version { get; } =
        typeof(SalesMcpHost).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    /// <summary>
    /// Creates a host builder with configuration, stderr-only logging, the sales services and the MCP
    /// server on the stdio transport.
    /// </summary>
    /// <param name="args">Command-line arguments, for example <c>--database ./sales.db</c>.</param>
    public static HostApplicationBuilder CreateBuilder(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Read appsettings.json from next to the executable, not from wherever the host launched us.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });

        // Later sources win: appsettings.json, then SALESMCP_ variables, then the command line.
        builder.Configuration.AddEnvironmentVariables(EnvironmentPrefix);
        builder.Configuration.AddCommandLine(args, _switches);

        // stdout carries the MCP protocol. Every log line, at every level, goes to stderr.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

        var settings = ServerSettings.From(builder.Configuration);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSalesInfrastructure(settings.ConnectionString);

        builder.Services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = ServerName, Version = Version })
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(SalesMcpHost).Assembly)
            .WithResourcesFromAssembly(typeof(SalesMcpHost).Assembly);

        return builder;
    }

    /// <summary>
    /// Creates the database folder, applies migrations and seeds the demo data on first run. Call it
    /// before the host starts, so the first request never waits on a migration.
    /// </summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var settings = services.GetRequiredService<ServerSettings>();
        Directory.CreateDirectory(Path.GetDirectoryName(settings.DatabasePath)!);

        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
