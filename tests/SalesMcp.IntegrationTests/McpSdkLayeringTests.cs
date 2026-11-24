using System.Reflection;

namespace SalesMcp.IntegrationTests;

/// <summary>Architecture guard (ADR-0002, ADR-0004): only host projects reference the MCP SDK.</summary>
public class McpSdkLayeringTests
{
    public static TheoryData<string> InnerLayers() =>
    [
        typeof(SalesMcp.Domain.AssemblyMarker).Assembly.GetName().Name!,
        typeof(SalesMcp.Application.AssemblyMarker).Assembly.GetName().Name!,
        typeof(SalesMcp.Infrastructure.AssemblyMarker).Assembly.GetName().Name!,
    ];

    [Theory]
    [MemberData(nameof(InnerLayers))]
    public void Inner_layers_do_not_reference_the_mcp_sdk(string assembly)
    {
        var references = Assembly.Load(assembly).GetReferencedAssemblies().Select(a => a.Name!);

        Assert.DoesNotContain(references, name => name.StartsWith("ModelContextProtocol", StringComparison.Ordinal));
    }

    [Fact]
    public void The_stdio_host_references_the_mcp_sdk()
    {
        var references = typeof(SalesMcp.Server.Stdio.SalesMcpHost).Assembly.GetReferencedAssemblies().Select(a => a.Name!);

        Assert.Contains(references, name => name.StartsWith("ModelContextProtocol", StringComparison.Ordinal));
    }
}
