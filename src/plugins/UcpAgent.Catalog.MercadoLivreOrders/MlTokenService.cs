using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace UcpAgent.Catalog.MercadoLivreOrders;

public sealed class MlTokenService(
    IHttpClientFactory httpFactory,
    IMemoryCache cache,
    IConfiguration config)
{
    private const string CacheKey = "ml_access_token";

    private string ClientId     => config["MercadoLivre:ClientId"]     ?? throw new InvalidOperationException("MercadoLivre:ClientId nao configurado");
    private string ClientSecret => config["MercadoLivre:ClientSecret"] ?? throw new InvalidOperationException("MercadoLivre:ClientSecret nao configurado");
    private string RedirectUri  => config["MercadoLivre:RedirectUri"]  ?? "http://localhost:5020/callback";

    // Troca authorization code por access_token
    public async Task<MlTokenResponse> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient("MlAuth");
        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["code"]          = code,
            ["redirect_uri"]  = RedirectUri
        });

        var response = await client.PostAsync("https://api.mercadolibre.com/oauth/token", payload, ct);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<MlTokenResponse>(cancellationToken: ct)
                    ?? throw new InvalidOperationException("Resposta de token invalida");

        // Armazena em cache com margem de 5 min
        var expiry = TimeSpan.FromSeconds(token.ExpiresIn - 300);
        cache.Set(CacheKey, token, expiry);
        cache.Set("ml_refresh_token", token.RefreshToken, TimeSpan.FromDays(180));

        return token;
    }

    // Obtem token valido (renova automaticamente)
    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out MlTokenResponse? cached) && cached != null)
            return cached.AccessToken;

        // Tenta refresh
        if (cache.TryGetValue("ml_refresh_token", out string? refreshToken) && refreshToken != null)
            return await RefreshAsync(refreshToken, ct);

        throw new InvalidOperationException("Nenhum token ML disponivel. Autorize a aplicacao primeiro via /api/ml/auth");
    }

    private async Task<string> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("MlAuth");
        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["refresh_token"] = refreshToken
        });

        var response = await client.PostAsync("https://api.mercadolibre.com/oauth/token", payload, ct);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<MlTokenResponse>(cancellationToken: ct)
                    ?? throw new InvalidOperationException("Refresh de token invalido");

        var expiry = TimeSpan.FromSeconds(token.ExpiresIn - 300);
        cache.Set(CacheKey, token, expiry);
        cache.Set("ml_refresh_token", token.RefreshToken, TimeSpan.FromDays(180));

        return token.AccessToken;
    }

    public string GetAuthorizationUrl()
    {
        return $"https://auth.mercadolivre.com.br/authorization?response_type=code&client_id={ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}";
    }
}

public record MlTokenResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("access_token")]  string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string RefreshToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")]    int    ExpiresIn,
    [property: System.Text.Json.Serialization.JsonPropertyName("user_id")]       long   UserId
);

