using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtpNet;

namespace UcpAgent.Api.Auth;

/// <summary>Serviço TOTP: gera secrets, valida códigos e emite/valida temp-tokens de 2FA.</summary>
[ExcludeFromCodeCoverage]
public sealed class TotpService(IConfiguration config)
{
    private readonly string _issuer   = config["Totp:Issuer"]   ?? "Comprai";
    private readonly string _jwtSecret = config["Jwt:Secret"]   ?? "comprai-dev-secret-change-in-prod";
    private readonly string _jwtIssuer = config["Jwt:Issuer"]   ?? "comprai-api";
    private readonly string _jwtAud   = config["Jwt:Audience"]  ?? "comprai-web";
    private static readonly TimeSpan TempTokenTtl = TimeSpan.FromMinutes(5);

    // ── secret ───────────────────────────────────────────────────────────

    /// <summary>Gera novo secret TOTP (20 bytes, base32).</summary>
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    /// <summary>Monta URI otpauth:// para gerar QR Code no frontend.</summary>
    public string BuildOtpAuthUri(string secret, string userEmail) =>
        $"otpauth://totp/{Uri.EscapeDataString(_issuer)}:{Uri.EscapeDataString(userEmail)}" +
        $"?secret={secret}&issuer={Uri.EscapeDataString(_issuer)}&algorithm=SHA1&digits=6&period=30";

    // ── validação ─────────────────────────────────────────────────────────

    /// <summary>Valida código TOTP de 6 dígitos. Aceita janela ±1 step (30 s).</summary>
    public bool Verify(string base32Secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6) return false;
        try
        {
            var key  = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(key);
            return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch { return false; }
    }

    // ── temp token (step-up JWT com scope=2fa) ────────────────────────────

    /// <summary>Emite JWT de curta duração (5 min) para completar o fluxo 2FA.</summary>
    public string GenerateTempToken(string customerId)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             _jwtIssuer,
            audience:           _jwtAud,
            claims:             [
                new Claim(JwtRegisteredClaimNames.Sub, customerId),
                new Claim("scope", "2fa"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires:            DateTime.UtcNow.Add(TempTokenTtl),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Valida tempToken e extrai customerId.
    /// Retorna null se inválido, expirado ou sem scope=2fa.
    /// </summary>
    public string? ValidateTempToken(string tempToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var principal = handler.ValidateToken(tempToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidIssuer              = _jwtIssuer,
                ValidAudience            = _jwtAud,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            }, out _);
            var scope = principal.FindFirstValue("scope");
            if (scope != "2fa") return null;
            return principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
        catch { return null; }
    }
}
