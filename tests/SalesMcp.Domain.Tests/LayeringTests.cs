
namespace SalesMcp.Domain.Tests;

/// <summary>
/// Architecture guard (ADR-0004): the Domain layer must not depend on any other SalesMcp layer.
/// </summary>
public class LayeringTests
{
    [Fact]
    public void Domain_does_not_reference_other_SalesMcp_assemblies()
    {
        var references = typeof(AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(name => name is not null && name.StartsWith("SalesMcp.", StringComparison.Ordinal));

        Assert.Empty(references);
    }

    [Fact]
    public void Domain_references_only_the_base_class_library()
    {
        // No EF Core, no MCP SDK, no packages at all: persistence is mapped from the outside (ADR-0004).
        var references = typeof(AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.All(references, name => Assert.True(
            name.StartsWith("System.", StringComparison.Ordinal) || name is "System" or "netstandard" or "mscorlib",
            $"Domain references {name}, which is not part of the base class library."));
    }
}
