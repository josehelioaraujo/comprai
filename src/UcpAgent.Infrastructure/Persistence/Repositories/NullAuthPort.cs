using System.Diagnostics.CodeAnalysis;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

/// <summary>Stub no-op para quando UsarPostgres=false (testes / mock mode).</summary>
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
}
