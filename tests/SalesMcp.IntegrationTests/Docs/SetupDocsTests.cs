using System.Text.RegularExpressions;
using SalesMcp.IntegrationTests.Harness;

namespace SalesMcp.IntegrationTests.Docs;

/// <summary>The setup guides only work if the demo accounts and SKUs they name exist in the seed.</summary>
public sealed partial class SetupDocsTests
{
    private static readonly string _root = FindRepositoryRoot();

    public static TheoryData<string> Documents() =>
        ["README.md", "docs/setup/claude-desktop.md", "docs/setup/claude-code.md", "docs/setup/demo.md"];

    [Theory]
    [MemberData(nameof(Documents))]
    public void Every_account_and_sku_named_is_in_the_demo_data(string document)
    {
        var text = File.ReadAllText(Path.Combine(_root, document));
        var accounts = McpServerFixture.Seeded.Customers.Select(c => c.AccountNumber.Value).ToHashSet(StringComparer.Ordinal);
        var skus = McpServerFixture.Seeded.Offerings.Select(o => o.Sku.Value).ToHashSet(StringComparer.Ordinal);

        Assert.All(Account().Matches(text).Select(m => m.Value), a => Assert.Contains(a, accounts));
        Assert.All(Sku().Matches(text).Select(m => m.Value), s => Assert.Contains(s, skus));
    }

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SalesMcp.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Could not find the repository root (SalesMcp.slnx).");
    }

    // Account numbers are three letters and a three-digit rank, such as KES-001.
    [GeneratedRegex(@"(?<![A-Z0-9-])[A-Z]{3}-\d{3}(?![\d-])")]
    private static partial Regex Account();

    [GeneratedRegex(@"ACM-[A-Z]{3}-\d{3}")]
    private static partial Regex Sku();
}
