namespace UcpAgent.SharedKernel.Ports;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? Document = null);

public record SsoCallbackRequest(
    string Provider,
    string ProviderId,
    string Name,
    string Email,
    string? AvatarUrl = null);

public record AuthCustomerDto(
    string Id,
    string Name,
    string Email,
    string Provider,
    string? AvatarUrl,
    bool EmailVerified);

public interface IAuthPort
{
    /// <summary>Cadastro por email/senha (credentials).</summary>
    Task<AuthCustomerDto?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Login por email/senha. Retorna null se credenciais inválidas.</summary>
    Task<AuthCustomerDto?> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Upsert por provider+providerId (Google, GitHub, Microsoft…).</summary>
    Task<AuthCustomerDto> SsoCallbackAsync(SsoCallbackRequest request, CancellationToken ct = default);

    /// <summary>Busca customer pelo id (para GET /api/auth/me).</summary>
    Task<AuthCustomerDto?> GetByIdAsync(string customerId, CancellationToken ct = default);
}
