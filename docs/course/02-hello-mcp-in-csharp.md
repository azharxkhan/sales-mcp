# Chapter 2: Hello MCP in C#

> **Milestone:** M2 · **Code:** PRs [#78](https://github.com/azharxkhan/sales-mcp/pull/78) to [#82](https://github.com/azharxkhan/sales-mcp/pull/82) · **Previous:** [Chapter 1](01-what-is-mcp.md) · **Later:** [Chapter 5: Real tools](05-real-tools.md)

## What you'll build

An MCP server over stdio with **one tool**, `search_catalog`, connected to Claude. This is the repository as it was when it had exactly that. To see the same code:

```bash
git checkout c9b84fc
```

Later versions have more tools; `git switch main` brings you back.

## Why it matters

The protocol part of an MCP server is small. Getting it small, and keeping it that way, is the point: the business logic you built in chapters 3 and 4 shouldn't know MCP exists.

## Concepts

**The official C# SDK** is the `ModelContextProtocol` NuGet package, pinned at 2.4.0 ([ADR-0002](../adr/0002-official-mcp-csharp-sdk.md)). It handles JSON-RPC, discovery and schemas.

**The generic host** gives configuration, logging and dependency injection, as in any .NET service.

**Thin adapters.** A tool class takes the arguments, calls one use case in the Application layer and maps the result. Only the host project references the SDK ([ADR-0004](../adr/0004-clean-layered-architecture.md)); `Inner_layers_do_not_reference_the_mcp_sdk` fails the build pipeline if that changes.

## Step by step

### Step 1: the project

The host is a console app with two packages:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Hosting" />
  <PackageReference Include="ModelContextProtocol" />
</ItemGroup>
```

Versions live in `Directory.Packages.props`, so the projects name no versions.

### Step 2: the server

The heart of `SalesMcpHost` is a few lines, shortened here:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP protocol. Every log line, at every level, goes to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddScoped<SearchCatalog>();

builder.Services
    .AddMcpServer(options => options.ServerInfo = new Implementation { Name = "sales-mcp", Version = Version })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

`WithToolsFromAssembly` finds every class marked `[McpServerToolType]`.

The real host does two things before it runs: it migrates and seeds the database, so the first request never waits, and it binds the session to the customer named by `--customer`. If it can't, it exits with code 2 and says why on stderr ([ADR-0016](../adr/0016-customer-bound-to-session.md)). It also reads `appsettings.json` from next to the executable, because a host starts the server from a folder you can't predict. For the same reason the database defaults to a per-user data folder.

### Step 3: the tool

```csharp
[McpServerToolType]
public sealed class CatalogTools(SearchCatalog searchCatalog, ISessionContext session)
{
    [McpServerTool(Name = "search_catalog", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Finds products, services and plans in this business's catalog by SKU, name or description...
        It never returns prices or stock...
        """)]
    public async Task<CatalogSearchResult> SearchCatalogAsync(
        [Description("Words to find in the SKU, name or description, for example \"6204 bearing\"...")]
        string query,
        [Description("Optional: only return this type, one of \"product\", \"service\" or \"plan\"...")]
        string? type = null,
        // limit and cursor
        CancellationToken cancellationToken = default)
    {
        var result = await searchCatalog
            .ExecuteAsync(session.BusinessId, new CatalogSearch(query, type, limit, cursor), cancellationToken);

        return result.IsSuccess ? CatalogSearchResult.From(result.Value) : throw ToolError.From(result.Error);
    }
}
```

From this method the SDK builds what the model sees:

| In C# | In the protocol |
|---|---|
| `[Description]` on the method and parameters | The tool's and arguments' descriptions |
| Parameter types, and which have defaults | `inputSchema`, with `query` required |
| The return record, with `UseStructuredContent` | `structuredContent` in the result |
| `ReadOnly`, `Idempotent`, `OpenWorld` | The annotations |
| Constructor parameters and `CancellationToken` | Nothing: they come from the server, not the model |

The last row matters. The business comes from `session`, so the model has no argument it could change to search someone else's catalog.

### Step 4: test it without a model

Three tests are worth having from the first tool:

- `Stdout_carries_only_json_rpc_messages` starts the real executable and parses every line it writes.
- `An_mcp_client_connects_and_lists_the_tools` connects the SDK's client over stdio and checks the negotiated protocol version.
- `Finds_an_offering_by_its_exact_sku_first` calls the tool through an in-process client, using [the harness](../../tests/SalesMcp.IntegrationTests/Harness/McpServerFixture.cs).

```bash
dotnet test --solution SalesMcp.slnx
```

### Step 5: connect it to Claude

Publish a copy, then point the host at it. In PowerShell:

```powershell
dotnet publish src/SalesMcp.Server.Stdio -c Release -o "$env:LOCALAPPDATA\Programs\sales-mcp-hello"
```

```powershell
claude mcp add --transport stdio sales-hello -- dotnet "$env:LOCALAPPDATA\Programs\sales-mcp-hello\SalesMcp.Server.Stdio.dll" --customer KES-001 --database "$env:LOCALAPPDATA\sales-mcp-hello\sales.db"
```

The separate `--database` matters if you've run a newer version: its database has a newer shape than this old code expects. Remove the server afterwards with `claude mcp remove sales-hello`. For Claude Desktop and other systems, see the [setup guides](../setup/claude-desktop.md).

Ask Claude to find a cordless drill. Then ask what it costs: at this version it has no way to know, and should say so.

## What can go wrong

| Mistake | What happens | The fix here |
|---|---|---|
| `dotnet run` as the client's command | Build output lands on stdout and breaks the protocol | Publish, then run the DLL |
| A log provider writing to stdout | The client fails to parse messages | Send every level to stderr |
| Relative paths in the server | It reads and writes wherever the host started it | Resolve from the executable and user folders |
| Migrating on the first request | The client times out on first use | Initialise before `RunAsync` |
| A database query in the tool class | Rules leak into the host and can't be reused over HTTP | One use case per tool |

## Check yourself

1. Why does `CatalogTools` take `ISessionContext` instead of a `businessId` parameter?
2. Which parts of the method signature does the model never see?
3. Why test the real executable as well as the in-process server?

**Exercise:** add a `ping_catalog` tool that returns how many offerings are on sale. Put the counting in an Application use case, and write the harness test first.

## Code

- At this version: [`SalesMcpHost.cs`](https://github.com/azharxkhan/sales-mcp/blob/c9b84fc/src/SalesMcp.Server.Stdio/SalesMcpHost.cs) and [`CatalogTools.cs`](https://github.com/azharxkhan/sales-mcp/blob/c9b84fc/src/SalesMcp.Server.Stdio/Tools/CatalogTools.cs)
- PRs: host [#78](https://github.com/azharxkhan/sales-mcp/pull/78), session [#79](https://github.com/azharxkhan/sales-mcp/pull/79), test harness [#81](https://github.com/azharxkhan/sales-mcp/pull/81), first tool [#82](https://github.com/azharxkhan/sales-mcp/pull/82)
