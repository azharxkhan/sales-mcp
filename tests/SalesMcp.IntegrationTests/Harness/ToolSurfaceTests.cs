namespace SalesMcp.IntegrationTests.Harness;

/// <summary>What the real server offers a host, with nothing added for tests.</summary>
public sealed class ToolSurfaceTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    /// <summary>
    /// The M2 tools, in name order. Each tool PR (#68 to #73) adds its name here, so the server can
    /// never offer a tool nobody reviewed. M3 turns this into a full contract snapshot (guardrail D5).
    /// </summary>
    private static readonly string[] _expectedTools = ["get_customer", "get_customer_credit", "search_catalog"];

    [Fact]
    public async Task Lists_exactly_the_expected_tools() =>
        Assert.Equal(_expectedTools, await server.ListToolNamesAsync());

    [Fact]
    public void Identifies_as_sales_mcp() => Assert.Equal("sales-mcp", server.Client.ServerInfo?.Name);
}
