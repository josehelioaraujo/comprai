using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Auth;

[ExcludeFromCodeCoverage]
public sealed class JwtService(IConfiguration config)
{
    private readonly string _secret   = config["Jwt:Secret"]   ?? "comprai-dev-secret-change-in-prod";
    private readonly string _issuer   = config["Jwt:Issuer"]   ?? "comprai-api";
    private readonly string _audience = config["Jwt:Audience"] ?? "comprai-web";
    private readonly int    _expMin   = int.TryParse(config["Jwt:ExpirationMinutes"], out var m) ? m : 15;

    /// <summary>Gera access_token JWT de curta duração (padrão 15 min).</summary>
    public string Generate(AuthCustomerDto customer)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   customer.Id),
            new Claim(JwtRegisteredClaimNames.Email, customer.Email),
            new Claim(JwtRegisteredClaimNames.Name,  customer.Name),
            new Claim("provider",                    customer.Provider),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_expMin),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Gera refresh token opaque (256 bits) e retorna (token_plain, hash_sha256).
    /// Armazena apenas o hash no BD — o plain vai para o cliente.
    /// </summary>
    public static (string Plain, string Hash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plain = Convert.ToBase64String(bytes);
        var hash  = Convert.ToBase64String(SHA256.HashData(bytes));
        return (plain, hash);
    }

    /// <summary>Computa hash SHA-256 de um refresh token recebido do cliente.</summary>
    public static string HashRefreshToken(string plain)
    {
        var bytes = Convert.FromBase64String(plain);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    public static string? GetCustomerId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
