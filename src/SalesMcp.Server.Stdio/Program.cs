// SalesMcp.Server.Stdio: the MCP server over stdio.
//
// stdout is reserved for protocol messages. Logs go to stderr, and the banned-API analyzer fails the
// build if code in this project or the layers below writes to Console.Out.

using Microsoft.Extensions.Hosting;
using SalesMcp.Server.Stdio;

using var host = SalesMcpHost.CreateBuilder(args).Build();
await SalesMcpHost.InitializeDatabaseAsync(host.Services);
await host.RunAsync();
