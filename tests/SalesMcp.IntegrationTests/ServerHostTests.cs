using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Server.Stdio;

namespace SalesMcp.IntegrationTests;

/// <summary>The stdio host's composition: settings, server identity and first-run database set-up.</summary>
public sealed class ServerHostTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "sales-mcp-tests", Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void The_database_defaults_to_a_per_user_data_folder()
    {
        var settings = Settings([]);

        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sales-mcp", "sales.db");
        Assert.Equal(expected, settings.DatabasePath);
        Assert.True(Path.IsPathRooted(settings.DatabasePath));
    }

    [Theory]
    [InlineData("--database")]
    [InlineData("--Database:Path")]
    public void The_command_line_sets_the_database_path(string option)
    {
        var path = Path.Combine(_folder, "custom.db");

        Assert.Equal(path, Settings([option, path]).DatabasePath);
    }

    [Fact]
    public void A_relative_database_path_is_made_absolute()
    {
        var settings = Settings(["--database", "relative.db"]);

        Assert.Equal(Path.GetFullPath("relative.db"), settings.DatabasePath);
    }

    [Fact]
    public void The_server_identifies_itself_by_name_and_version()
    {
        using var host = SalesMcpHost.CreateBuilder(["--database", Path.Combine(_folder, "sales.db")]).Build();

        var info = host.Services.GetRequiredService<IOptions<McpServerOptions>>().Value.ServerInfo!;

        Assert.Equal("sales-mcp", info.Name);
        Assert.StartsWith("0.1.0-dev", info.Version, StringComparison.Ordinal);
    }

    [Fact]
    public async Task First_run_creates_the_folder_migrates_and_seeds_and_later_runs_change_nothing()
    {
        var path = Path.Combine(_folder, "nested", "sales.db");
        using var host = SalesMcpHost.CreateBuilder(["--database", path]).Build();

        await SalesMcpHost.InitializeDatabaseAsync(host.Services, Ct);
        var first = await CountsAsync(host.Services);
        await SalesMcpHost.InitializeDatabaseAsync(host.Services, Ct);

        Assert.True(File.Exists(path));
        Assert.True(first.Customers > 0 && first.Quotes > 0);
        Assert.Equal(first, await CountsAsync(host.Services));
        await using var scope = host.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<SalesDbContext>().Database.GetPendingMigrationsAsync(Ct));
    }

    private static ServerSettings Settings(string[] args)
    {
        using var host = SalesMcpHost.CreateBuilder(args).Build();
        return host.Services.GetRequiredService<ServerSettings>();
    }

    private static async Task<(int Customers, int Quotes)> CountsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        return (await db.Customers.CountAsync(Ct), await db.Quotes.CountAsync(Ct));
    }
}
