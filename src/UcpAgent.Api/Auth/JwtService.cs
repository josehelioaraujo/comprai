using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Auth;

public sealed class JwtService(IConfiguration config)
{
    private readonly string _secret   = config["Jwt:Secret"]   ?? "comprai-dev-secret-change-in-prod";
    private readonly string _issuer   = config["Jwt:Issuer"]   ?? "comprai-api";
    private readonly string _audience = config["Jwt:Audience"] ?? "comprai-web";
    private readonly int    _expMin   = int.TryParse(config["Jwt:ExpirationMinutes"], out var m) ? m : 1440;

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

    public static string? GetCustomerId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
