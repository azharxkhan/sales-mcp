# Set up Sales MCP in Claude Desktop

This connects the server to Claude Desktop as one demo customer. When you finish, you can ask Claude to find a product, price it and create a quote. Using Claude Code instead? See [claude-code.md](claude-code.md).

## 1. Publish the server

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). From the repository root:

```powershell
dotnet publish src/SalesMcp.Server.Stdio -c Release -o "$env:LOCALAPPDATA\Programs\sales-mcp"
```

On macOS or Linux:

```bash
dotnet publish src/SalesMcp.Server.Stdio -c Release -o ~/sales-mcp
```

Publishing gives Claude a fixed copy to run. Don't point Claude at `dotnet run`: build output on stdout would break the protocol.

## 2. Add it to the config

In Claude Desktop, open the **Claude** menu → **Settings…** → **Developer** → **Edit Config**. That opens `claude_desktop_config.json`:

| System | Location |
|---|---|
| Windows | `%APPDATA%\Claude\claude_desktop_config.json` |
| macOS | `~/Library/Application Support/Claude/claude_desktop_config.json` |

Add the server. Use the full path to the DLL; in JSON, write each Windows backslash twice.

```json
{
  "mcpServers": {
    "sales-mcp": {
      "command": "dotnet",
      "args": [
        "C:\\Users\\you\\AppData\\Local\\Programs\\sales-mcp\\SalesMcp.Server.Stdio.dll",
        "--customer",
        "KES-001"
      ]
    }
  }
}
```

Then quit Claude Desktop completely and start it again.

## 3. Try it

Ask, for example:

> Find me a cordless drill, tell me the price for two, and quote me for two drills and two 4 Ah batteries.

[The demo](demo.md) shows what the server returns for these steps.

## Which customer

`--customer` is the account number of the customer the conversation serves. The model can't change it, and it only sees that customer's prices, quotes and orders. The server won't start without it. The demo data has 40 customers of the invented Acme Distributors, for example:

| Account | Customer | Why try it |
|---|---|---|
| `KES-001` | Kestrel Ridge Mining Supplies | The largest account: contract prices and its own price list |
| `BLU-002` | Blue Crane Engineering | Another large account with contract prices |
| `WAT-014` | Waterberg Agri Machinery | On credit hold |

You can set `SALESMCP_CUSTOMER` in the entry's `env` instead of passing `--customer`. `--agent` (or `SALESMCP_AGENT`) sets the agent named on quotes; it defaults to `acme-distributor-jolt`.

## The database

The first start creates a SQLite database and fills it with the demo data, dated from that day.

| System | Default location |
|---|---|
| Windows | `%LOCALAPPDATA%\sales-mcp\sales.db` |
| macOS | `~/Library/Application Support/sales-mcp/sales.db` |
| Linux | `~/.local/share/sales-mcp/sales.db` |

To keep it somewhere else, add `"--database", "C:\\path\\to\\sales.db"` to `args`.

**To reset it:** quit Claude Desktop, delete `sales.db` (and `sales.db-wal` and `sales.db-shm` if present), then start Claude Desktop again. Your quotes are deleted with it, and the demo data is recreated. Do this too if the database was made by an older build of the server and something looks out of date.

## Logs

The server logs to stderr, which Claude Desktop writes to files:

| System | Folder |
|---|---|
| Windows | `%APPDATA%\Claude\logs` |
| macOS | `~/Library/Logs/Claude` |

`mcp-server-sales-mcp.log` holds the server's own messages and `mcp.log` the connection problems.

## If it doesn't connect

- **"The server can't start. SessionCustomerNotSpecified" or "SessionCustomerUnknown"** in the log: add or correct `--customer`.
- **`dotnet` not found:** use its full path as `command`, for example `C:\\Program Files\\dotnet\\dotnet.exe`.
- **Nothing in the logs:** check the JSON is valid and that you restarted Claude Desktop fully.

## Debugging with MCP Inspector

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) calls the tools and resources without a model. It needs a recent [Node.js](https://nodejs.org).

```powershell
npx @modelcontextprotocol/inspector -e SALESMCP_CUSTOMER=KES-001 dotnet "$env:LOCALAPPDATA\Programs\sales-mcp\SalesMcp.Server.Stdio.dll"
```
