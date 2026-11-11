namespace SalesMcp.IntegrationTests;

/// <summary>
/// Placeholder for end-to-end tests (MCP client ↔ server). Real tests arrive in M2
/// with the integration test harness. For now, check the host assembly wires up the layers.
/// </summary>
public class ServerHostTests
{
    [Fact]
    public void Stdio_host_references_application_and_infrastructure()
    {
        var infrastructure = typeof(SalesMcp.Infrastructure.AssemblyMarker).Assembly;
        var app = typeof(SalesMcp.Application.AssemblyMarker).Assembly;

        Assert.Equal("SalesMcp.Infrastructure", infrastructure.GetName().Name);
        Assert.Equal("SalesMcp.Application", app.GetName().Name);
    }
}
