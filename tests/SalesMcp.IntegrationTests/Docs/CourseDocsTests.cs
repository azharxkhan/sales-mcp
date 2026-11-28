using System.Text;
using System.Text.RegularExpressions;

namespace SalesMcp.IntegrationTests.Docs;

/// <summary>
/// Keeps the README, setup guides, course chapters and domain glossary in step with the repository:
/// links and anchors resolve, the glossary defines the terms issue #50 asks for, and every test a
/// chapter names exists.
/// </summary>
public sealed partial class CourseDocsTests
{
    private static readonly string _root = FindRepositoryRoot();

    public static TheoryData<string> Documents()
    {
        var data = new TheoryData<string> { "README.md" };
        foreach (var folder in new[] { "docs/course", "docs/domain", "docs/mcp", "docs/setup" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(_root, folder), "*.md").Order(StringComparer.Ordinal))
            {
                data.Add(Path.GetRelativePath(_root, file).Replace('\\', '/'));
            }
        }

        return data;
    }

    [Theory]
    [InlineData("01-what-is-mcp.md")]
    [InlineData("02-hello-mcp-in-csharp.md")]
    [InlineData("05-real-tools.md")]
    public void The_m2_chapters_exist_are_indexed_and_have_the_chapter_shape(string chapter)
    {
        var text = File.ReadAllText(Path.Combine(_root, "docs/course", chapter));
        var index = File.ReadAllText(Path.Combine(_root, "docs/course/README.md"));

        Assert.Contains($"]({chapter})", index, StringComparison.Ordinal);
        string[] sections =
        [
            "## What you'll build", "## Why it matters", "## Concepts", "## Step by step",
            "## What can go wrong", "## Check yourself", "## Code",
        ];
        Assert.All(sections, s => Assert.Contains(s, text, StringComparison.Ordinal));
    }

    [Fact]
    public void The_m1_chapters_and_glossary_exist()
    {
        Assert.True(File.Exists(Path.Combine(_root, "docs/course/03-modelling-a-sales-domain.md")));
        Assert.True(File.Exists(Path.Combine(_root, "docs/course/04-persistence.md")));
        Assert.True(File.Exists(Path.Combine(_root, "docs/domain/glossary.md")));
    }

    [Theory]
    [MemberData(nameof(Documents))]
    public void Relative_links_and_anchors_resolve(string document)
    {
        var path = Path.Combine(_root, document);
        var broken = new List<string>();

        foreach (Match link in MarkdownLink().Matches(File.ReadAllText(path)))
        {
            var target = link.Groups["target"].Value;
            if (target.StartsWith("http://", StringComparison.Ordinal) || target.StartsWith("https://", StringComparison.Ordinal))
            {
                continue;
            }

            var hash = target.IndexOf('#', StringComparison.Ordinal);
            var file = hash < 0 ? target : target[..hash];
            var anchor = hash < 0 ? null : target[(hash + 1)..];
            var resolved = file.Length == 0 ? path : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, file));

            if (!File.Exists(resolved) && !Directory.Exists(resolved))
            {
                broken.Add($"{target} (no such file)");
            }
            else if (anchor is not null && !HeadingSlugs(resolved).Contains(anchor))
            {
                broken.Add($"{target} (no such heading)");
            }
        }

        Assert.True(broken.Count == 0, $"Broken links in {document}:\n" + string.Join('\n', broken));
    }

    [Theory]
    [InlineData("Quote")]
    [InlineData("Sales order")]
    [InlineData("Hold")]
    [InlineData("Back-order")]
    [InlineData("Allocation")]
    [InlineData("Credit exposure")]
    [InlineData("ATP")]
    public void The_glossary_defines_the_order_to_cash_terms(string term)
    {
        var glossary = File.ReadAllText(Path.Combine(_root, "docs/domain/glossary.md"));

        Assert.Contains($"| **{term}**", glossary, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Documents))]
    public void Every_test_a_chapter_names_exists(string document)
    {
        var tests = string.Concat(
            Directory.EnumerateFiles(Path.Combine(_root, "tests"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));

        var missing = TestName().Matches(File.ReadAllText(Path.Combine(_root, document)))
            .Select(m => m.Groups["method"].Value)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !tests.Contains($" {name}(", StringComparison.Ordinal) && !tests.Contains($" {name} =>", StringComparison.Ordinal))
            .ToList();

        Assert.True(missing.Count == 0, $"{document} names tests that don't exist: " + string.Join(", ", missing));
    }

    /// <summary>GitHub's heading anchors: lower case, punctuation dropped, spaces to hyphens.</summary>
    private static HashSet<string> HeadingSlugs(string file)
    {
        var slugs = new HashSet<string>(StringComparer.Ordinal);
        if (!file.EndsWith(".md", StringComparison.Ordinal))
        {
            return slugs;
        }

        foreach (var line in File.ReadLines(file))
        {
            if (!line.StartsWith('#'))
            {
                continue;
            }

            var slug = new StringBuilder();
            foreach (var c in line.TrimStart('#').Trim())
            {
                if (char.IsLetterOrDigit(c) || c is '-' or '_')
                {
                    slug.Append(char.ToLowerInvariant(c));
                }
                else if (c == ' ')
                {
                    slug.Append('-');
                }
            }

            slugs.Add(slug.ToString());
        }

        return slugs;
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

    [GeneratedRegex(@"\]\((?<target>[^)\s]+)\)")]
    private static partial Regex MarkdownLink();

    // Test names are sentences joined by underscores, in backticks, optionally after the class name.
    [GeneratedRegex(@"`(?:[A-Z]\w*Tests\.)?(?<method>[A-Z][a-z0-9]*(?:_[A-Za-z0-9]+){2,})`")]
    private static partial Regex TestName();
}
