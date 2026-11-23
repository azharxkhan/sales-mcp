using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Application.Persistence;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Infrastructure.Seeding;

namespace SalesMcp.Infrastructure;

/// <summary>Registers the data layer with a host's service container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the SQLite <see cref="SalesDbContext"/> and the Application persistence interfaces.
    /// All are scoped: one context per tool call. Also adds <see cref="AcmeSeeder"/>.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <param name="connectionString">A SQLite connection string, for example <c>Data Source=sales.db</c>.</param>
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<SalesDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<SalesReader>();
        services.AddScoped<ICatalogReader>(sp => sp.GetRequiredService<SalesReader>());
        services.AddScoped<ICustomerReader>(sp => sp.GetRequiredService<SalesReader>());
        services.AddScoped<IPricingReader>(sp => sp.GetRequiredService<SalesReader>());
        services.AddScoped<IStockReader>(sp => sp.GetRequiredService<SalesReader>());

        services.AddScoped<SalesDocuments>();
        services.AddScoped<IQuoteRepository>(sp => sp.GetRequiredService<SalesDocuments>());
        services.AddScoped<IOrderRepository>(sp => sp.GetRequiredService<SalesDocuments>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SalesDocuments>());

        services.AddScoped<AcmeSeeder>();

        return services;
    }
}
