namespace SalesMcp.Application.Tests;

/// <summary>
/// Architecture guard (ADR-0004): Application may depend on Domain only, never on
/// Infrastructure or a host project.
/// </summary>
public class LayeringTests
{
    [Fact]
    public void Application_references_only_the_Domain_layer()
    {
        var salesMcpReferences = typeof(AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(name => name is not null && name.StartsWith("SalesMcp.", StringComparison.Ordinal))
            .ToList();

        Assert.All(salesMcpReferences, name => Assert.Equal("SalesMcp.Domain", name));
    }
}
