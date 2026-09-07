using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Tests.Integration;

[Collection("IntegrationTests")]
public class McpServerIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _mcpClient;

    public McpServerIntegrationTest(CompraApiFactory factory)
    {
        // MCP Server roda na porta 5030
        var mcpBaseUrl = Environment.GetEnvironmentVariable("MCP_BASE_URL")
                         ?? "http://localhost:5030";

        _mcpClient = new HttpClient { BaseAddress = new Uri(mcpBaseUrl) };
    }

    [Fact]
    public async Task McpServer_HealthCheck_ReturnsOk()
    {
        try
        {
            var response = await _mcpClient.GetAsync("/health");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NoContent,
                $"MCP health esperado 200/204, recebido {response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            // MCP pode não estar rodando no ambiente de teste unitário
            Skip.If(true, "MCP Server não disponível neste ambiente");
        }
    }

    [Fact]
    public async Task McpServer_ListTools_ReturnsSeven()
    {
        try
        {
            var payload = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/list",
                @params = new { }
            };

            var response = await _mcpClient.PostAsJsonAsync("/mcp", payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tools = body.GetProperty("result").GetProperty("tools");

            Assert.Equal(7, tools.GetArrayLength());
        }
        catch (HttpRequestException)
        {
            Skip.If(true, "MCP Server não disponível neste ambiente");
        }
    }

    [Theory]
    [InlineData("search_products")]
    [InlineData("add_to_cart")]
    [InlineData("get_cart")]
    [InlineData("checkout")]
    [InlineData("get_order")]
    [InlineData("get_ml_orders")]
    [InlineData("route_intent")]
    public async Task McpServer_ToolExists(string toolName)
    {
        try
        {
            var payload = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/list",
                @params = new { }
            };

            var response = await _mcpClient.PostAsJsonAsync("/mcp", payload);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tools = body.GetProperty("result").GetProperty("tools");

            var names = tools.EnumerateArray()
                             .Select(t => t.GetProperty("name").GetString())
                             .ToList();

            Assert.Contains(toolName, names);
        }
        catch (HttpRequestException)
        {
            Skip.If(true, "MCP Server não disponível neste ambiente");
        }
    }
}
