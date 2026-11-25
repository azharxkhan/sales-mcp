using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.Server.Stdio;

namespace SalesMcp.IntegrationTests.Harness;

/// <summary>
/// The real server composition, running in-process over a pair of in-memory pipes, with an SDK
/// <see cref="McpClient"/> connected to it. Tests call tools and read resources exactly as a host
/// would: by name, with JSON arguments.
/// </summary>
/// <remarks>
/// One fixture per test class (<c>IClassFixture</c>): a fresh SQLite file, migrated and seeded with
/// Acme, and a session bound to <see cref="CustomerAccount"/>. Tests in a class share it, so tests
/// that change data must not depend on each other's changes.
/// </remarks>
public class McpServerFixture : IAsyncLifetime
{
    /// <summary>The day the seed is built for in tests that need its data in memory.</summary>
    public static readonly DateOnly SeedDay = new(2026, 11, 19);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "sales-mcp-tests", Guid.NewGuid().ToString("N"));
    private readonly Pipe _clientToServer = new();
    private readonly Pipe _serverToClient = new();
    private IHost? _host;
    private McpClient? _client;

    /// <summary>The connected client.</summary>
    public McpClient Client => _client ?? throw new InvalidOperationException("The fixture hasn't started.");

    /// <summary>The server's services, for checking what a tool wrote to the database.</summary>
    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("The fixture hasn't started.");

    /// <summary>
    /// The account the session is bound to. The top Acme customer by default; override it in a
    /// derived fixture to test as another seeded customer.
    /// </summary>
    protected virtual string CustomerAccount => Seeded.Customers.Single(c => c.Id == AcmeSeed.Manifest.Customers.Top).AccountNumber.Value;

    /// <summary>The seed dataset, built in memory, to look up fixture rows and expected values.</summary>
    public static SeedDataset Seeded { get; } = AcmeSeed.Build(SeedDay);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        string[] args =
        [
            "--database", Path.Combine(_folder, "sales.db"),
            "--customer", CustomerAccount,
            "--Logging:LogLevel:Default", "Warning",
        ];

        var builder = SalesMcpHost.CreateBuilder(args, server =>
        {
            server.WithStreamServerTransport(_clientToServer.Reader.AsStream(), _serverToClient.Writer.AsStream());
            ConfigureServer(server);
        });

        _host = builder.Build();
        await SalesMcpHost.InitializeDatabaseAsync(_host.Services, Ct);
        if (!await SalesMcpHost.BindSessionAsync(_host.Services, Ct))
        {
            throw new InvalidOperationException($"The fixture couldn't bind the session to {CustomerAccount}.");
        }

        await _host.StartAsync(Ct);
        _client = await McpClient.CreateAsync(
            new StreamClientTransport(_clientToServer.Writer.AsStream(), _serverToClient.Reader.AsStream()),
            cancellationToken: Ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        await _clientToServer.Writer.CompleteAsync();
        await _serverToClient.Writer.CompleteAsync();

        if (_host is not null)
        {
            await _host.StopAsync(CancellationToken.None);
            _host.Dispose();
        }

        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>Extra server configuration for a derived fixture, such as test-only tools.</summary>
    protected virtual void ConfigureServer(IMcpServerBuilder server)
    {
    }

    /// <summary>The names of the tools the server lists, in order. Empty while it offers no tools.</summary>
    public async Task<IReadOnlyList<string>> ListToolNamesAsync()
    {
        if (Client.ServerCapabilities?.Tools is null)
        {
            return [];
        }

        var tools = await Client.ListToolsAsync(cancellationToken: Ct);
        return [.. tools.Select(t => t.Name).Order(StringComparer.Ordinal)];
    }

    /// <summary>Calls a tool that must succeed and returns its structured result as <typeparamref name="T"/>.</summary>
    /// <param name="name">The tool name, for example <c>get_price</c>.</param>
    /// <param name="arguments">The arguments, as an object or dictionary serialised to JSON, for example <c>new { offering = "ACM-BRG-6204", quantity = 12 }</c>.</param>
    public async Task<T> CallToolAsync<T>(string name, object? arguments = null)
    {
        var result = await CallAsync(name, arguments);
        Assert.False(result.IsError is true, $"{name} failed: {TextOf(result)}");
        Assert.True(result.StructuredContent is not null, $"{name} returned no structured content: {TextOf(result)}");

        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(result.StructuredContent), McpHarnessJson.Options)
            ?? throw new InvalidOperationException($"{name} returned null structured content.");
    }

    /// <summary>Calls a tool that must fail with <paramref name="code"/>, and returns the error.</summary>
    /// <remarks>
    /// The code is read from the structured content's <c>code</c> field if there is one, otherwise
    /// from the text, so the helper works with either way of reporting a tool error.
    /// </remarks>
    public async Task<ToolCallError> ExpectToolErrorAsync(string name, object? arguments, string code)
    {
        var result = await CallAsync(name, arguments);
        var text = TextOf(result);
        Assert.True(result.IsError is true, $"{name} was expected to fail with {code} but succeeded: {text}");

        var structuredCode = result.StructuredContent is null
            ? null
            : JsonSerializer.SerializeToElement(result.StructuredContent).TryGetProperty("code", out var c) ? c.GetString() : null;

        Assert.True(
            structuredCode == code || text.Contains(code, StringComparison.Ordinal),
            $"{name} failed, but not with {code}: {structuredCode ?? text}");
        return new ToolCallError(structuredCode ?? code, text);
    }

    /// <summary>Reads a resource and returns its text.</summary>
    public async Task<string> ReadResourceAsync(string uri)
    {
        var result = await Client.ReadResourceAsync(uri, cancellationToken: Ct);
        return string.Concat(result.Contents.OfType<TextResourceContents>().Select(c => c.Text));
    }

    private Task<CallToolResult> CallAsync(string name, object? arguments)
    {
        var json = JsonSerializer.SerializeToElement(arguments ?? new { }, McpHarnessJson.Options);
        var dictionary = json.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.Clone(), StringComparer.Ordinal);
        return Client.CallToolAsync(name, dictionary, cancellationToken: Ct).AsTask();
    }

    private static string TextOf(CallToolResult result) =>
        string.Join('\n', result.Content.OfType<TextContentBlock>().Select(b => b.Text));
}

/// <summary>A tool error as the client saw it.</summary>
/// <param name="Code">The error code.</param>
/// <param name="Text">The text the model would read.</param>
public sealed record ToolCallError(string Code, string Text);

/// <summary>JSON settings that match how the SDK writes tool results: camelCase, case-insensitive.</summary>
public static class McpHarnessJson
{
    /// <summary>The options.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
