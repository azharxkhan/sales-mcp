using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Client;

namespace SalesMcp.IntegrationTests;

/// <summary>
/// Launches the real stdio executable as a child process, as an MCP host would. Slower than the
/// in-process tests, but it is the only way to prove what reaches stdout.
/// </summary>
public sealed class StdioServerProcessTests : IDisposable
{
    private static readonly string _server = Path.Combine(AppContext.BaseDirectory, "SalesMcp.Server.Stdio.dll");
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "sales-mcp-tests", Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string DatabasePath => Path.Combine(_folder, "sales.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public async Task Stdout_carries_only_json_rpc_messages()
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in new[] { _server, "--database", DatabasePath })
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stderr = process.StandardError.ReadToEndAsync(Ct);

        await process.StandardInput.WriteLineAsync("""{"jsonrpc":"2.0","id":1,"method":"ping"}""".AsMemory(), Ct);
        await process.StandardInput.FlushAsync(Ct);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        var reply = await process.StandardOutput.ReadLineAsync(timeout.Token);

        // Closing stdin is how a host ends a stdio session; the server must exit cleanly.
        process.StandardInput.Close();
        var rest = await process.StandardOutput.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);

        var lines = new[] { reply }.Concat(rest.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToList();
        Assert.All(lines, line =>
        {
            using var message = JsonDocument.Parse(line!);
            Assert.Equal("2.0", message.RootElement.GetProperty("jsonrpc").GetString());
        });
        using (var ping = JsonDocument.Parse(reply!))
        {
            Assert.Equal(1, ping.RootElement.GetProperty("id").GetInt32());
            Assert.True(ping.RootElement.TryGetProperty("result", out _), reply);
        }

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("Seeded the Acme Distributors demo data", await stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_mcp_client_connects_and_reads_the_server_identity()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "sales-mcp",
            Command = "dotnet",
            Arguments = [_server, "--database", DatabasePath],
        });

        // Connecting runs discovery, which replaced the initialize handshake in spec 2026-07-28.
        await using var client = await McpClient.CreateAsync(transport, cancellationToken: Ct);

        Assert.Equal("2026-07-28", client.NegotiatedProtocolVersion);

        var server = Assert.IsType<ModelContextProtocol.Protocol.Implementation>(client.ServerInfo);
        Assert.Equal("sales-mcp", server.Name);
        Assert.StartsWith("0.1.0-dev", server.Version, StringComparison.Ordinal);

        // No tools are registered yet, so the server doesn't offer tools/list. The first tool (#68) adds it.
        Assert.Null(client.ServerCapabilities?.Tools);
    }
}
