using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Adapters;

public sealed class EfiPaymentAdapter(
    EfiPayOptions opts,
    IHttpClientFactory factory,
    IMemoryCache cache,
    ILogger<EfiPaymentAdapter> logger) : IPaymentPort
{
    private static readonly JsonSerializerOptions _json =
        new(JsonSerializerDefaults.Web);

    public async Task<PaymentResultDto> ProcessAsync(
        string orderId, decimal amount, string currency,
        PaymentMethodDto method,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(method.Provider, "efipay", StringComparison.OrdinalIgnoreCase))
            return new PaymentResultDto(
                PaymentId: "", Success: false, Status: "unsupported",
                PixQrCode: null, PixCopiaECola: null,
                Error: $"Provider '{method.Provider}' não suportado por EfiPaymentAdapter");

        try
        {
            var token  = await GetAccessTokenAsync(cancellationToken);
            var client = factory.CreateClient("efipay-pix");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var safe = orderId.Replace("-", "");
            var txid = $"comprai{(safe.Length >= 25 ? safe[..25] : safe.PadRight(25, '0'))}";

            var body = JsonSerializer.Serialize(new
            {
                calendario   = new { expiracao = 3600 },
                devedor      = new { nome = "Comprador", cpf  = "00000000000" },
                valor        = new { original = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                chave        = opts.ChavePix,
                solicitacaoPagador = $"Pedido {orderId}"
            }, _json);

            var response = await client.PutAsync(
                $"/v2/cob/{txid}",
                new StringContent(body, Encoding.UTF8, "application/json"),
                cancellationToken);

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Efí Pay cob falhou — status={Status} body={Body}",
                    response.StatusCode, raw);
                return new PaymentResultDto(
                    PaymentId: "", Success: false, Status: response.StatusCode.ToString(),
                    PixQrCode: null, PixCopiaECola: null, Error: raw);
            }

            using var doc = JsonDocument.Parse(raw);
            var root      = doc.RootElement;
            var txidResp  = root.GetProperty("txid").GetString() ?? txid;
            var locId     = root.TryGetProperty("loc", out var loc)
                            && loc.TryGetProperty("id", out var lid)
                            ? lid.GetInt64().ToString()
                            : "";

            string? copiaECola = null;
            string? qrCode     = null;

            if (!string.IsNullOrEmpty(locId))
            {
                var qrResp = await client.GetAsync($"/v2/loc/{locId}/qrcode", cancellationToken);
                if (qrResp.IsSuccessStatusCode)
                {
                    var qrRaw = await qrResp.Content.ReadAsStringAsync(cancellationToken);
                    using var qrDoc = JsonDocument.Parse(qrRaw);
                    var qrRoot      = qrDoc.RootElement;
                    copiaECola = qrRoot.TryGetProperty("pixCopiaECola", out var pce)
                        ? pce.GetString() : null;
                    qrCode = qrRoot.TryGetProperty("imagemQrcode", out var img)
                        ? img.GetString()
                        : $"{opts.BaseUrl}/v2/loc/{locId}/qrcode/imagem";
                }
            }

            return new PaymentResultDto(
                PaymentId: txidResp,
                Success:   true,
                Status:    "ATIVA",
                PixQrCode: qrCode,
                PixCopiaECola: copiaECola,
                Error:     null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar pagamento Efí Pay — orderId={OrderId}", orderId);
            return new PaymentResultDto(
                PaymentId: "", Success: false, Status: "error",
                PixQrCode: null, PixCopiaECola: null, Error: ex.Message);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        const string cacheKey = "efipay_access_token";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
            return cached;

        var client      = factory.CreateClient("efipay-pix");
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{opts.ClientId}:{opts.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(
            "{\"grant_type\":\"client_credentials\"}",
            Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, ct);
        var raw      = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Efí Pay OAuth falhou: {raw}");

        using var doc     = JsonDocument.Parse(raw);
        var token         = doc.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn     = doc.RootElement.GetProperty("expires_in").GetInt32();
        var cacheOptions  = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(expiresIn - 60));

        cache.Set(cacheKey, token, cacheOptions);
        return token;
    }
}
