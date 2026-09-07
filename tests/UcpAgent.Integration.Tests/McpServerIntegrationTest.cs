using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Integration.Tests;

[Collection("IntegrationTests")]
public class McpServerIntegrationTest
{
    private readonly HttpClient _mcpClient;
    private readonly bool _disponivel;

    public McpServerIntegrationTest()
    {
        var mcpBaseUrl = Environment.GetEnvironmentVariable("MCP_BASE_URL") ?? "";
        _disponivel = !string.IsNullOrEmpty(mcpBaseUrl);
        _mcpClient = new HttpClient { BaseAddress = new Uri(_disponivel ? mcpBaseUrl : "http://localhost:5030") };
    }

    [Fact(Skip = "MCP Server requer MCP_BASE_URL configurado e VPS rodando")]
    public async Task McpServer_ListTools_ReturnsSeven()
    {
        var payload = new { jsonrpc = "2.0", id = 1, method = "tools/list", @params = new { } };
        var response = await _mcpClient.PostAsJsonAsync("/mcp", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tools = body.GetProperty("result").GetProperty("tools");
        Assert.Equal(7, tools.GetArrayLength());
    }
}
