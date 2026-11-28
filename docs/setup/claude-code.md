# Set up Sales MCP in Claude Code

This connects the server to Claude Code as one demo customer. When you finish, you can ask Claude to find a product, price it and create a quote. Using Claude Desktop instead? See [claude-desktop.md](claude-desktop.md).

## 1. Publish the server

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). From the repository root:

```powershell
dotnet publish src/SalesMcp.Server.Stdio -c Release -o "$env:LOCALAPPDATA\Programs\sales-mcp"
```

On macOS or Linux:

```bash
dotnet publish src/SalesMcp.Server.Stdio -c Release -o ~/sales-mcp
```

## 2. Add the server

Everything after `--` is the command Claude Code runs:

```powershell
claude mcp add --transport stdio sales-mcp -- dotnet "$env:LOCALAPPDATA\Programs\sales-mcp\SalesMcp.Server.Stdio.dll" --customer KES-001
```

On macOS or Linux:

```bash
claude mcp add --transport stdio sales-mcp -- dotnet ~/sales-mcp/SalesMcp.Server.Stdio.dll --customer KES-001
```

This adds it for you, in the current project only. Add `--scope user` before the name to have it in all your projects.

## 3. Check and try it

```powershell
claude mcp list
```

Inside a session, `/mcp` shows the server and its status. Then ask, for example:

> Find me a cordless drill, tell me the price for two, and quote me for two drills and two 4 Ah batteries.

[The demo](demo.md) shows what the server returns for these steps. To remove the server:

```powershell
claude mcp remove sales-mcp
```

## Which customer

`--customer` is the account number of the customer the conversation serves. The model can't change it, and the server won't start without it. `KES-001` is the largest demo account; [the Claude Desktop guide](claude-desktop.md#which-customer) lists others and the other settings.

## The database

The same as for Claude Desktop: `%LOCALAPPDATA%\sales-mcp\sales.db` on Windows, `~/Library/Application Support/sales-mcp/sales.db` on macOS, `~/.local/share/sales-mcp/sales.db` on Linux. Pass `--database <path>` after the DLL to use another file.

**To reset it:** close the sessions using the server, then delete `sales.db` (and `sales.db-wal` and `sales.db-shm` if present). The next start recreates the demo data; quotes you created are deleted.

## Logs

The server logs to stderr. To capture Claude Code's debug output, including MCP connection problems, start it with a log file of your choosing and search it for `sales-mcp`:

```powershell
claude --debug-file sales-mcp-debug.log
```

If the server fails to start, run it yourself to read the reason. It waits for protocol messages when it starts correctly; stop it with Ctrl+C.

```powershell
dotnet "$env:LOCALAPPDATA\Programs\sales-mcp\SalesMcp.Server.Stdio.dll" --customer KES-001
```

## Debugging with MCP Inspector

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) calls the tools and resources without a model. It needs a recent [Node.js](https://nodejs.org).

```powershell
npx @modelcontextprotocol/inspector -e SALESMCP_CUSTOMER=KES-001 dotnet "$env:LOCALAPPDATA\Programs\sales-mcp\SalesMcp.Server.Stdio.dll"
```
