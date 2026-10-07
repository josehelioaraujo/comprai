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

public record UpdateProfileRequest(
    string CustomerId,
    string Name,
    string? Phone    = null,
    string? Document = null);

public record SaveAddressRequest(
    string  CustomerId,
    string  ZipCode,
    string  Street,
    string? Number       = null,
    string? Complement   = null,
    string? Neighborhood = null,
    string  City         = "",
    string  State        = "",
    string? Label        = null,
    bool    IsDefault    = true);

public record CustomerAddressDto(
    string  Id,
    string  Label,
    string  ZipCode,
    string  Street,
    string? Number,
    string? Complement,
    string? Neighborhood,
    string  City,
    string  State,
    bool    IsDefault);

public record AuthCustomerDto(
    string Id,
    string Name,
    string Email,
    string Provider,
    string? AvatarUrl,
    bool EmailVerified,
    string? Phone    = null,
    string? Document = null,
    IReadOnlyList<CustomerAddressDto>? Addresses = null);

public interface IAuthPort
{
    Task<AuthCustomerDto?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthCustomerDto?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthCustomerDto> SsoCallbackAsync(SsoCallbackRequest request, CancellationToken ct = default);

    /// <summary>Retorna customer com endereços para GET /api/auth/me.</summary>
    Task<AuthCustomerDto?> GetByIdAsync(string customerId, CancellationToken ct = default);

    /// <summary>Atualiza nome, telefone e documento do customer.</summary>
    Task<AuthCustomerDto?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>Salva ou substitui endereço padrão do customer.</summary>
    Task<CustomerAddressDto?> SaveAddressAsync(SaveAddressRequest request, CancellationToken ct = default);

    // ── V059-F2: Refresh Token ────────────────────────────────────────────

    /// <summary>
    /// Salva hash do refresh token e expiração no customer.
    /// Chamado após login/register/sso bem-sucedido.
    /// </summary>
    Task SaveRefreshTokenAsync(string customerId, string tokenHash, DateTime expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Busca customer pelo hash do refresh token se ainda válido.
    /// Retorna null se não encontrado ou expirado.
    /// </summary>
    Task<AuthCustomerDto?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Invalida o refresh token atual (logout ou rotação).</summary>
    Task RevokeRefreshTokenAsync(string customerId, CancellationToken ct = default);

    // ── V060-F1: Email Verification ──────────────────────────────────────

    /// <summary>
    /// Persiste o código OTP de 6 caracteres e sua expiração no customer.
    /// Chamado antes de disparar o e-mail via Resend.
    /// </summary>
    Task SaveVerificationCodeAsync(string customerId, string code, DateTime expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Valida o código OTP: se correto e não expirado, marca email_verified=TRUE e apaga o token.
    /// Retorna true em caso de sucesso, false caso contrário.
    /// </summary>
    Task<bool> VerifyEmailCodeAsync(string customerId, string code, CancellationToken ct = default);
}
