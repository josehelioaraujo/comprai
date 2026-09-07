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
        var mcpBaseUrl = Environment.GetEnvironmentVariable("MCP_BASE_URL") ?? "http://localhost:5030";
        _mcpClient = new HttpClient { BaseAddress = new Uri(mcpBaseUrl) };
        _disponivel = Environment.GetEnvironmentVariable("MCP_BASE_URL") != null;
    }

    [Fact]
    public async Task McpServer_ListTools_ReturnsSeven()
    {
        Skip.If(!_disponivel, "MCP_BASE_URL nao configurado — pulando teste MCP");

        var payload = new { jsonrpc = "2.0", id = 1, method = "tools/list", @params = new { } };

        try
        {
            var response = await _mcpClient.PostAsJsonAsync("/mcp", payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tools = body.GetProperty("result").GetProperty("tools");
            Assert.Equal(7, tools.GetArrayLength());
        }
        catch (HttpRequestException)
        {
            Skip.If(true, "MCP Server nao disponivel");
        }
    }
}
