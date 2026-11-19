using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Infrastructure;
using SalesMcp.Infrastructure.Persistence;

namespace SalesMcp.IntegrationTests.Persistence;

/// <summary>
/// A private in-memory SQLite database, migrated with the real migrations and wired up through
/// <see cref="DependencyInjection.AddSalesInfrastructure"/>. Kept alive by one open connection.
/// </summary>
public sealed class SqliteDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly ServiceProvider _services;

    private SqliteDatabase(string connectionString)
    {
        _keepAlive = new SqliteConnection(connectionString);
        _services = new ServiceCollection().AddSalesInfrastructure(connectionString).BuildServiceProvider();
    }

    public static async Task<SqliteDatabase> CreateAsync()
    {
        var database = new SqliteDatabase($"Data Source=sales-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await database._keepAlive.OpenAsync(TestContext.Current.CancellationToken);
        await database.WithContextAsync(db => db.Database.MigrateAsync(TestContext.Current.CancellationToken));
        return database;
    }

    /// <summary>Runs <paramref name="work"/> in a fresh scope, as one tool call would.</summary>
    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = _services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    public Task InScopeAsync(Func<IServiceProvider, Task> work) =>
        InScopeAsync<bool>(async sp =>
        {
            await work(sp);
            return true;
        });

    public Task WithContextAsync(Func<SalesDbContext, Task> work) =>
        InScopeAsync(sp => work(sp.GetRequiredService<SalesDbContext>()));

    /// <summary>Adds entities in one scope and saves them.</summary>
    public Task SaveAsync(params object[] entities) =>
        WithContextAsync(async db =>
        {
            db.AddRange(entities);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _keepAlive.DisposeAsync();
    }
}
