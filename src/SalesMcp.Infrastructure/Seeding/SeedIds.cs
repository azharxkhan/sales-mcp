using System.Security.Cryptography;
using System.Text;

namespace SalesMcp.Infrastructure.Seeding;

/// <summary>
/// Stable ids for seeded rows. Each id is derived from a readable key such as
/// <c>acme/offering/ACM-PTL-101</c>, so the same row gets the same id on every machine and every run,
/// and tests can name a fixture without querying for it first.
/// </summary>
internal static class SeedIds
{
    /// <summary>A version 8 (custom) UUID built from the SHA-256 hash of <paramref name="key"/>.</summary>
    public static Guid For(string key)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(key), hash);

        var bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80); // version 8
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 9562 variant
        return new Guid(bytes, bigEndian: true);
    }
}
