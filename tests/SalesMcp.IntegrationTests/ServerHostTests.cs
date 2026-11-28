using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using SalesMcp.Application.Session;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.Server.Stdio;

namespace SalesMcp.IntegrationTests;

/// <summary>The stdio host's composition: settings, server identity, first-run database set-up and session binding.</summary>
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
        Assert.Equal("0.1.0", info.Version.Split('+')[0]);
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

    [Theory]
    [InlineData("--customer")]
    [InlineData("--Customer")]
    public void The_command_line_names_the_customer(string option) =>
        Assert.Equal("KES-001", Settings([option, " KES-001 "]).CustomerAccount);

    [Fact]
    public void No_customer_is_configured_by_default() => Assert.Null(Settings([]).CustomerAccount);

    [Fact]
    public async Task Binds_the_session_to_the_configured_customer()
    {
        var top = TopCustomer();
        using var host = await StartedHostAsync("--customer", top.AccountNumber.Value.ToLowerInvariant());

        Assert.True(await SalesMcpHost.BindSessionAsync(host.Services, Ct));

        var session = host.Services.GetRequiredService<ISessionContext>();
        Assert.Equal(top.Id, session.CustomerId);
        Assert.Equal(top.BusinessId, session.BusinessId);
        Assert.Equal(top.AccountNumber, session.AccountNumber);
        Assert.Same(session, host.Services.GetRequiredService<SessionContext>());
    }

    [Theory]
    [InlineData("XYZ-999")]
    [InlineData(null)]
    public async Task An_unknown_or_missing_customer_leaves_the_session_unbound(string? account)
    {
        using var host = await StartedHostAsync(account is null ? [] : ["--customer", account]);

        Assert.False(await SalesMcpHost.BindSessionAsync(host.Services, Ct));
        Assert.False(host.Services.GetRequiredService<SessionContext>().IsBound);
    }

    [Fact]
    public async Task The_session_cannot_be_bound_twice()
    {
        using var host = await StartedHostAsync("--customer", TopCustomer().AccountNumber.Value);
        Assert.True(await SalesMcpHost.BindSessionAsync(host.Services, Ct));

        await Assert.ThrowsAsync<InvalidOperationException>(() => SalesMcpHost.BindSessionAsync(host.Services, Ct));
    }

    internal static Domain.Customers.Customer TopCustomer() =>
        AcmeSeed.Build(new DateOnly(2026, 11, 19)).Customers.Single(c => c.Id == AcmeSeed.Manifest.Customers.Top);

    private async Task<Microsoft.Extensions.Hosting.IHost> StartedHostAsync(params string[] args)
    {
        var host = SalesMcpHost.CreateBuilder(["--database", Path.Combine(_folder, "sales.db"), .. args]).Build();
        await SalesMcpHost.InitializeDatabaseAsync(host.Services, Ct);
        return host;
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
