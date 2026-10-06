using Dapper;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class CustomerAuthRepository(IDbConnectionFactory db) : IAuthPort
{
    public async Task<AuthCustomerDto?> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        using var conn = db.Create();
        var exists = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM customer WHERE email = @Email)",
            new { req.Email });
        if (exists) return null;

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        var row = await conn.QuerySingleAsync<CustomerRow>(
            """
            INSERT INTO customer (name, email, document, provider, provider_id, password_hash, email_verified, channel)
            VALUES (@Name, @Email, @Document, 'credentials', NULL, @Hash, FALSE, 'web')
            RETURNING id, name, email, provider, avatar_url, email_verified
            """,
            new { req.Name, req.Email, req.Document, Hash = hash });

        return ToDto(row);
    }

    public async Task<AuthCustomerDto?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<CustomerRow>(
            """
            SELECT id, name, email, provider, avatar_url, email_verified, password_hash
            FROM customer
            WHERE email = @Email AND provider = 'credentials'
            """,
            new { Email = email });

        if (row is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, row.PasswordHash)) return null;

        return ToDto(row);
    }

    public async Task<AuthCustomerDto> SsoCallbackAsync(SsoCallbackRequest req, CancellationToken ct = default)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleAsync<CustomerRow>(
            """
            INSERT INTO customer (name, email, provider, provider_id, avatar_url, email_verified, channel)
            VALUES (@Name, @Email, @Provider, @ProviderId, @AvatarUrl, TRUE, 'web')
            ON CONFLICT (provider, provider_id) DO UPDATE
                SET name        = EXCLUDED.name,
                    avatar_url  = EXCLUDED.avatar_url,
                    updated_at  = NOW()
            RETURNING id, name, email, provider, avatar_url, email_verified
            """,
            new { req.Name, req.Email, req.Provider, ProviderId = req.ProviderId, req.AvatarUrl });

        return ToDto(row);
    }

    public async Task<AuthCustomerDto?> GetByIdAsync(string customerId, CancellationToken ct = default)
    {
        using var conn = db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<CustomerRow>(
            "SELECT id, name, email, provider, avatar_url, email_verified FROM customer WHERE id = @Id",
            new { Id = customerId });

        return row is null ? null : ToDto(row);
    }

    private static AuthCustomerDto ToDto(CustomerRow r) =>
        new(r.Id.ToString(), r.Name, r.Email, r.Provider, r.AvatarUrl, r.EmailVerified);

    private sealed class CustomerRow
    {
        public Guid    Id            { get; init; }
        public string  Name          { get; init; } = "";
        public string  Email         { get; init; } = "";
        public string  Provider      { get; init; } = "";
        public string? AvatarUrl     { get; init; }
        public bool    EmailVerified { get; init; }
        public string? PasswordHash  { get; init; }
    }
}
