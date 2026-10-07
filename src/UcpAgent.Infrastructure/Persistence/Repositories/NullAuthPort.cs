using System.Diagnostics.CodeAnalysis;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage]
public sealed class NullAuthPort : IAuthPort
{
    public Task<AuthCustomerDto?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => Task.FromResult<AuthCustomerDto?>(null);

    public Task<AuthCustomerDto?> LoginAsync(string email, string password, CancellationToken ct = default)
        => Task.FromResult<AuthCustomerDto?>(null);

    public Task<AuthCustomerDto> SsoCallbackAsync(SsoCallbackRequest request, CancellationToken ct = default)
        => Task.FromResult(new AuthCustomerDto(Guid.NewGuid().ToString(), request.Name, request.Email, request.Provider, request.AvatarUrl, true));

    public Task<AuthCustomerDto?> GetByIdAsync(string customerId, CancellationToken ct = default)
        => Task.FromResult<AuthCustomerDto?>(null);

    public Task<AuthCustomerDto?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
        => Task.FromResult<AuthCustomerDto?>(null);

    public Task<CustomerAddressDto?> SaveAddressAsync(SaveAddressRequest request, CancellationToken ct = default)
        => Task.FromResult<CustomerAddressDto?>(null);

    // V059-F2: Refresh Token — no-op no Null Object
    public Task SaveRefreshTokenAsync(string customerId, string tokenHash, DateTime expiresAt, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<AuthCustomerDto?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => Task.FromResult<AuthCustomerDto?>(null);

    public Task RevokeRefreshTokenAsync(string customerId, CancellationToken ct = default)
        => Task.CompletedTask;

    // V060-F1: Email Verification — no-op no Null Object
    public Task SaveVerificationCodeAsync(string customerId, string code, DateTime expiresAt, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<bool> VerifyEmailCodeAsync(string customerId, string code, CancellationToken ct = default)
        => Task.FromResult(false);
}
