using UcpAgent.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ HTTP Client para chamar a UcpAgent.Api â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddHttpClient("CompraApi", client =>
{
    var baseUrl = builder.Configuration["CompraApi:BaseUrl"]
                  ?? "http://localhost:5020";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// â”€â”€ MCP Server via SSE â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SearchProductsTool>()
    .WithTools<CartTool>()
    .WithTools<CheckoutTool>()
    .WithTools<OrderTool>()
    .WithTools<IntentTool>();

var app = builder.Build();

app.MapMcp("/mcp");
app.MapGet("/health", () => Results.Ok(new { status = "ok", server = "UcpAgent MCP Server", version = "1.3.0" }));

app.Run();
