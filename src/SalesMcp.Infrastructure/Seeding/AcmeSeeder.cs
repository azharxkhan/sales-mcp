using Microsoft.EntityFrameworkCore;
using SalesMcp.Infrastructure.Persistence;

namespace SalesMcp.Infrastructure.Seeding;

/// <summary>Writes the <see cref="AcmeSeed"/> dataset to the database once.</summary>
/// <param name="db">The context to write to. The schema must already be migrated.</param>
public sealed class AcmeSeeder(SalesDbContext db)
{
    /// <summary>
    /// Seeds Acme Distributors as of <paramref name="today"/> unless it is already there. Safe to call on
    /// every start-up: a second call changes nothing.
    /// </summary>
    /// <returns><see langword="true"/> if rows were written; <see langword="false"/> if Acme already existed.</returns>
    public async Task<bool> SeedAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var business = AcmeSeed.Manifest.Business;
        if (await db.Businesses.AnyAsync(b => b.Id == business, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        db.AddRange(AcmeSeed.Build(today).AllRows());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
