
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
}
