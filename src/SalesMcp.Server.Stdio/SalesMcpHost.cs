using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using SalesMcp.Application.Session;
using SalesMcp.Infrastructure;
using SalesMcp.Infrastructure.Persistence;

namespace SalesMcp.Server.Stdio;

/// <summary>
/// Composes the stdio MCP server. Kept apart from <c>Program</c> so the integration tests can build
/// the same composition the executable runs.
/// </summary>
public static partial class SalesMcpHost
{
    /// <summary>The name the server reports to MCP clients.</summary>
    public const string ServerName = "sales-mcp";

    /// <summary>The prefix for environment variables, for example <c>SALESMCP_Database__Path</c>.</summary>
    public const string EnvironmentPrefix = "SALESMCP_";

    private static readonly Dictionary<string, string> _switches = new(StringComparer.OrdinalIgnoreCase)
    {
        ["--database"] = ServerSettings.DatabasePathKey,
        ["--customer"] = ServerSettings.CustomerKey,
    };

    /// <summary>The server's version, from the assembly's informational version.</summary>
    public static string Version { get; } =
        typeof(SalesMcpHost).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    /// <summary>
    /// Creates a host builder with configuration, stderr-only logging, the session, the sales services
    /// and the MCP server on the stdio transport.
    /// </summary>
    /// <param name="args">Command-line arguments, for example <c>--database ./sales.db</c>.</param>
    public static HostApplicationBuilder CreateBuilder(string[] args) =>
        CreateBuilder(args, server => server.WithStdioServerTransport());

    /// <summary>
    /// Creates the same host with a different transport. The integration tests use this to run the
    /// real composition over in-memory streams.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="transport">Adds the transport, for example <c>WithStreamServerTransport</c>.</param>
    public static HostApplicationBuilder CreateBuilder(string[] args, Action<IMcpServerBuilder> transport)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(transport);

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

        // One stdio process serves one session, so the session context is a singleton.
        builder.Services.AddSingleton<SessionContext>();
        builder.Services.AddSingleton<ISessionContext>(sp => sp.GetRequiredService<SessionContext>());
        builder.Services.AddScoped<SessionBinder>();

        var server = builder.Services
            .AddMcpServer(options => options.ServerInfo = new Implementation { Name = ServerName, Version = Version })
            .WithToolsFromAssembly(typeof(SalesMcpHost).Assembly)
            .WithResourcesFromAssembly(typeof(SalesMcpHost).Assembly);
        transport(server);

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

    /// <summary>
    /// Binds the session to the configured customer. Call it after <see cref="InitializeDatabaseAsync"/>
    /// and before the host starts, so no tool can run unbound.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the session is bound; <see langword="false"/> if not, after logging
    /// why to stderr. The server should then exit.
    /// </returns>
    public static async Task<bool> BindSessionAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var settings = services.GetRequiredService<ServerSettings>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SalesMcpHost).FullName!);

        await using var scope = services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<SessionBinder>()
            .ResolveAsync(settings.CustomerAccount, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            LogBindFailed(logger, result.Error.Code, result.Error.Message);
            return false;
        }

        services.GetRequiredService<SessionContext>().Bind(result.Value);
        LogBound(logger, result.Value.AccountNumber.Value);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "The server can't start. {Code}: {Message}")]
    private static partial void LogBindFailed(ILogger logger, string code, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Session bound to customer {AccountNumber}.")]
    private static partial void LogBound(ILogger logger, string accountNumber);
}
