namespace UcpAgent.Api.RateLimit;

public class RateLimitOptions
{
    public FixedWindowOptions FixedWindow { get; set; } = new();
    public AuthWindowOptions  Auth        { get; set; } = new();
}

public class FixedWindowOptions
{
    public int PermitLimit   { get; set; } = 100;
    public int WindowSeconds { get; set; } = 10;
    public int QueueLimit    { get; set; } = 0;
}

/// <summary>
/// Limite para endpoints de autenticação (login/register).
/// Padrão: 5 tentativas / 60 s — adequado para produção.
/// Para testes E2E, aumente via env: RateLimit__Auth__PermitLimit=30
/// </summary>
public class AuthWindowOptions
{
    public int PermitLimit   { get; set; } = 5;
    public int WindowSeconds { get; set; } = 60;
}
