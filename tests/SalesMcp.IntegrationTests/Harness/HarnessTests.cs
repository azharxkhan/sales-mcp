using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using SalesMcp.Application.Session;
using SalesMcp.Infrastructure.Seeding;

namespace SalesMcp.IntegrationTests.Harness;

/// <summary>
/// Proves the harness itself, with test-only tools: a tool gets the bound session through dependency
/// injection, and the helpers read results, errors and resources the way the real tools will use them.
/// </summary>
public sealed class HarnessTests(HarnessTests.ProbeServer server) : IClassFixture<HarnessTests.ProbeServer>
{
    [Fact]
    public async Task A_tool_sees_the_session_bound_at_start_up()
    {
        var top = McpServerFixture.Seeded.Customers.Single(c => c.Id == AcmeSeed.Manifest.Customers.Top);

        var session = await server.CallToolAsync<SessionEcho>("probe_session");

        Assert.Equal(top.AccountNumber.Value, session.AccountNumber);
        Assert.Equal(top.Id.Value, session.CustomerId);
    }

    [Fact]
    public async Task Arguments_arrive_as_json()
    {
        var echo = await server.CallToolAsync<Echoed>("probe_echo", new { text = "ACM-BRG-6204", quantity = 12.5m });

        Assert.Equal(new Echoed("ACM-BRG-6204", 12.5m), echo);
    }

    [Fact]
    public async Task A_tool_error_is_read_by_its_code()
    {
        var error = await server.ExpectToolErrorAsync("probe_fail", new { code = "ProbeFailed" }, "ProbeFailed");

        Assert.Equal("ProbeFailed", error.Code);
        Assert.Contains("The probe failed on purpose.", error.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Calling_a_tool_that_fails_as_if_it_succeeded_fails_the_test()
    {
        var failure = await Assert.ThrowsAnyAsync<Exception>(() => server.CallToolAsync<Echoed>("probe_fail", new { code = "ProbeFailed" }));

        Assert.Contains("probe_fail failed", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Expecting_the_wrong_error_code_fails_the_test()
    {
        var failure = await Assert.ThrowsAnyAsync<Exception>(() => server.ExpectToolErrorAsync("probe_fail", new { code = "ProbeFailed" }, "SomethingElse"));

        Assert.Contains("not with SomethingElse", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reads_a_resource()
    {
        var text = await server.ReadResourceAsync("probe://session");

        Assert.Equal(McpServerFixture.Seeded.Customers.Single(c => c.Id == AcmeSeed.Manifest.Customers.Top).AccountNumber.Value, text);
    }

    [Fact]
    public async Task Lists_the_probe_tools()
    {
        Assert.Equal(["probe_echo", "probe_fail", "probe_session"], await server.ListToolNamesAsync());
    }

    /// <summary>The real server plus the probe tools and resource.</summary>
    public sealed class ProbeServer : McpServerFixture
    {
        protected override void ConfigureServer(IMcpServerBuilder server) =>
            server.WithTools<ProbeTools>().WithResources<ProbeResources>();
    }

    public sealed record SessionEcho(string AccountNumber, Guid CustomerId);

    public sealed record Echoed(string Text, decimal Quantity);

    [McpServerToolType]
    public sealed class ProbeTools(ISessionContext session)
    {
        [McpServerTool(Name = "probe_session", ReadOnly = true, UseStructuredContent = true)]
        [Description("Test only: returns the bound session.")]
        public SessionEcho ProbeSession() => new(session.AccountNumber.Value, session.CustomerId.Value);

        [McpServerTool(Name = "probe_echo", ReadOnly = true, UseStructuredContent = true)]
        [Description("Test only: returns its arguments.")]
        public static Echoed ProbeEcho(string text, decimal quantity) => new(text, quantity);

        [McpServerTool(Name = "probe_fail", ReadOnly = true, UseStructuredContent = true)]
        [Description("Test only: always fails with the given code.")]
        public static Echoed ProbeFail(string code) => throw new McpException($"{code}: The probe failed on purpose.");
    }

    [McpServerResourceType]
    public sealed class ProbeResources(ISessionContext session)
    {
        [McpServerResource(UriTemplate = "probe://session", Name = "probe_session", MimeType = "text/plain")]
        [Description("Test only: the bound account number.")]
        public string Session() => session.AccountNumber.Value;
    }
}
