namespace SalesMcp.Domain.Tests;

/// <summary>
/// Checks that the banned-API list (src/BannedSymbols.txt) is wired into every src project,
/// so reading the clock, creating an unseeded Random or writing to stdout fails the build.
/// </summary>
public class BannedApiTests
{
    [Theory]
    [InlineData("SalesMcp.Domain")]
    [InlineData("SalesMcp.Application")]
    [InlineData("SalesMcp.Infrastructure")]
    [InlineData("SalesMcp.Server.Stdio")]
    public void Every_src_project_references_the_banned_api_list(string project)
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "src", project, project + ".csproj"));

        Assert.Contains("Microsoft.CodeAnalysis.BannedApiAnalyzers", csproj, StringComparison.Ordinal);
        Assert.Contains("BannedSymbols.txt", csproj, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("P:System.DateTime.Now")]
    [InlineData("P:System.DateTime.UtcNow")]
    [InlineData("P:System.DateTimeOffset.UtcNow")]
    [InlineData("M:System.Random.#ctor")]
    [InlineData("T:System.Console")]
    public void Banned_list_contains(string symbol)
    {
        var banned = File.ReadAllLines(Path.Combine(FindRepoRoot(), "src", "BannedSymbols.txt"));

        Assert.Contains(banned, line => line.StartsWith(symbol + ";", StringComparison.Ordinal));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SalesMcp.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root (SalesMcp.slnx) not found.");
    }
}
