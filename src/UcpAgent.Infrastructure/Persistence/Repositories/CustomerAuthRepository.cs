using System.Diagnostics.CodeAnalysis;
using Dapper;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage]
public sealed class CustomerAuthRepository(IDbConnectionFactory db) : IAuthPort
{
    public async Task<AuthCustomerDto?> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var exists = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM customer WHERE email = @Email)",
            new { req.Email });
        if (exists) return null;

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var row = await conn.QuerySingleAsync<CustomerRow>(
            """
            INSERT INTO customer (name, email, document, provider, provider_id, password_hash, email_verified, channel)
            VALUES (@Name, @Email, @Document, 'credentials', NULL, @Hash, FALSE, 'web')
            RETURNING id, name, email, provider, avatar_url, email_verified, phone, document, totp_enabled
            """,
            new { req.Name, req.Email, req.Document, Hash = hash });
        return ToDto(row, null);
    }

    public async Task<AuthCustomerDto?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<CustomerRow>(
            """
            SELECT id, name, email, provider, avatar_url, email_verified, phone, document, password_hash, totp_enabled
            FROM customer
            WHERE email = @Email AND provider = 'credentials'
            """,
            new { Email = email });
        if (row is null) return null;
        if (row.PasswordHash is null) return null;   // usuário SSO sem senha
        if (!BCrypt.Net.BCrypt.Verify(password, row.PasswordHash)) return null;
        return ToDto(row, null);
    }

    public async Task<AuthCustomerDto> SsoCallbackAsync(SsoCallbackRequest req, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var row = await conn.QuerySingleAsync<CustomerRow>(
            """
            INSERT INTO customer (name, email, provider, provider_id, avatar_url, email_verified, channel)
            VALUES (@Name, @Email, @Provider, @ProviderId, @AvatarUrl, TRUE, 'web')
            ON CONFLICT (provider, provider_id) DO UPDATE
                SET name        = EXCLUDED.name,
                    avatar_url  = EXCLUDED.avatar_url,
                    updated_at  = NOW()
            RETURNING id, name, email, provider, avatar_url, email_verified, phone, document, totp_enabled
            """,
            new { req.Name, req.Email, req.Provider, ProviderId = req.ProviderId, req.AvatarUrl });
        return ToDto(row, null);
    }

    public async Task<AuthCustomerDto?> GetByIdAsync(string customerId, CancellationToken ct = default)
    {
        var guid = Guid.Parse(customerId);
        using var conn = await db.CreateAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<CustomerRow>(
            """
            SELECT id, name, email, provider, avatar_url, email_verified, phone, document, totp_enabled
            FROM customer WHERE id = @Id
            """,
            new { Id = guid });
        if (row is null) return null;

        var addresses = (await conn.QueryAsync<AddressRow>(
            """
            SELECT id, label, zip_code, street, number, complement, neighborhood, city, state, is_default
            FROM customer_address
            WHERE customer_id = @Id
            ORDER BY is_default DESC, created_at DESC
            """,
            new { Id = guid })).ToList();

        return ToDto(row, addresses);
    }

    public async Task<AuthCustomerDto?> UpdateProfileAsync(UpdateProfileRequest req, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<CustomerRow>(
            """
            UPDATE customer
            SET name       = @Name,
                phone      = @Phone,
                document   = @Document,
                updated_at = NOW()
            WHERE id = @Id
            RETURNING id, name, email, provider, avatar_url, email_verified, phone, document, totp_enabled
            """,
            new { req.Name, req.Phone, req.Document, Id = Guid.Parse(req.CustomerId) });
        return row is null ? null : ToDto(row, null);
    }

    public async Task<CustomerAddressDto?> SaveAddressAsync(SaveAddressRequest req, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);

        var customerGuid = Guid.Parse(req.CustomerId);
        if (req.IsDefault)
            await conn.ExecuteAsync(
                "UPDATE customer_address SET is_default = FALSE WHERE customer_id = @Id",
                new { Id = customerGuid });

        var row = await conn.QuerySingleOrDefaultAsync<AddressRow>(
            """
            INSERT INTO customer_address
                (customer_id, label, zip_code, street, number, complement, neighborhood, city, state, is_default)
            VALUES
                (@CustomerId, @Label, @ZipCode, @Street, @Number, @Complement, @Neighborhood, @City, @State, @IsDefault)
            ON CONFLICT (customer_id, zip_code, number) DO UPDATE
                SET label        = EXCLUDED.label,
                    street       = EXCLUDED.street,
                    complement   = EXCLUDED.complement,
                    neighborhood = EXCLUDED.neighborhood,
                    city         = EXCLUDED.city,
                    state        = EXCLUDED.state,
                    is_default   = EXCLUDED.is_default
            RETURNING id, label, zip_code, street, number, complement, neighborhood, city, state, is_default
            """,
            new {
                CustomerId   = customerGuid,
                Label        = req.Label ?? "principal",
                req.ZipCode, req.Street, req.Number, req.Complement,
                req.Neighborhood, req.City, req.State, req.IsDefault
            });
        return row is null ? null : ToAddressDto(row);
    }

    // ── V059-F2: Refresh Token ────────────────────────────────────────────────

    public async Task SaveRefreshTokenAsync(string customerId, string tokenHash, DateTime expiresAt, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE customer
            SET refresh_token_hash       = @Hash,
                refresh_token_expires_at = @ExpiresAt,
                updated_at               = NOW()
            WHERE id = @Id
            """,
            new { Hash = tokenHash, ExpiresAt = expiresAt, Id = Guid.Parse(customerId) });
    }

    public async Task<AuthCustomerDto?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<CustomerRow>(
            """
            SELECT id, name, email, provider, avatar_url, email_verified, phone, document, totp_enabled
            FROM customer
            WHERE refresh_token_hash = @Hash
              AND refresh_token_expires_at > NOW()
            """,
            new { Hash = tokenHash });
        return row is null ? null : ToDto(row, null);
    }

    public async Task RevokeRefreshTokenAsync(string customerId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE customer
            SET refresh_token_hash       = NULL,
                refresh_token_expires_at = NULL,
                updated_at               = NOW()
            WHERE id = @Id
            """,
            new { Id = Guid.Parse(customerId) });
    }

    // ── V060-F1: Email Verification ──────────────────────────────────────────

    public async Task SaveVerificationCodeAsync(string customerId, string code, DateTime expiresAt, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE customer
            SET email_verification_token      = @Code,
                email_verification_expires_at = @ExpiresAt,
                updated_at                    = NOW()
            WHERE id = @Id
            """,
            new { Code = code, ExpiresAt = expiresAt, Id = Guid.Parse(customerId) });
    }

    public async Task<bool> VerifyEmailCodeAsync(string customerId, string code, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var affected = await conn.ExecuteAsync(
            """
            UPDATE customer
            SET email_verified                = TRUE,
                email_verification_token      = NULL,
                email_verification_expires_at = NULL,
                updated_at                    = NOW()
            WHERE id                          = @Id
              AND email_verification_token      = @Code
              AND email_verification_expires_at > NOW()
            """,
            new { Id = Guid.Parse(customerId), Code = code });
        return affected > 0;
    }

    // ── V060-F2: TOTP 2FA ─────────────────────────────────────────────────────

    public async Task SaveTotpSecretAsync(string customerId, string secret, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE customer
            SET totp_secret = @Secret,
                updated_at  = NOW()
            WHERE id = @Id
            """,
            new { Secret = secret, Id = Guid.Parse(customerId) });
    }

    public async Task<(bool Enabled, string? Secret)> GetTotpDataAsync(string customerId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<(bool Enabled, string? Secret)>(
            "SELECT totp_enabled, totp_secret FROM customer WHERE id = @Id",
            new { Id = Guid.Parse(customerId) });
        return row;
    }

    public async Task<bool> EnableTotpAsync(string customerId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var affected = await conn.ExecuteAsync(
            """
            UPDATE customer
            SET totp_enabled = TRUE,
                updated_at   = NOW()
            WHERE id = @Id
            """,
            new { Id = Guid.Parse(customerId) });
        return affected > 0;
    }

    public async Task DisableTotpAsync(string customerId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE customer
            SET totp_enabled = FALSE,
                totp_secret  = NULL,
                updated_at   = NOW()
            WHERE id = @Id
            """,
            new { Id = Guid.Parse(customerId) });
    }

    // ── V061-F1: Recuperação de Senha ────────────────────────────────────────

    public async Task<bool> SavePasswordResetTokenByEmailAsync(string email, string tokenHash, DateTime expiresAt, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var affected = await conn.ExecuteAsync(
            """
            UPDATE customer
            SET password_reset_token_hash = @TokenHash,
                password_reset_expires_at  = @ExpiresAt,
                updated_at                 = NOW()
            WHERE email = @Email
            """,
            new { TokenHash = tokenHash, ExpiresAt = expiresAt, Email = email });
        return affected > 0;
    }

    public async Task<AuthCustomerDto?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<CustomerRow>(
            """
            SELECT id, name, email, provider, avatar_url, email_verified, phone, document, totp_enabled
            FROM customer
            WHERE password_reset_token_hash = @TokenHash
              AND password_reset_expires_at  > NOW()
            """,
            new { TokenHash = tokenHash });
        return row is null ? null : ToDto(row, null);
    }

    public async Task<bool> ResetPasswordAsync(string tokenHash, string newPasswordPlain, CancellationToken ct = default)
    {
        var newHash = BCrypt.Net.BCrypt.HashPassword(newPasswordPlain);
        using var conn = await db.CreateAsync(ct);
        var affected = await conn.ExecuteAsync(
            """
            UPDATE customer
            SET password_hash              = @NewHash,
                password_reset_token_hash  = NULL,
                password_reset_expires_at  = NULL,
                updated_at                 = NOW()
            WHERE password_reset_token_hash = @TokenHash
              AND password_reset_expires_at  > NOW()
            """,
            new { TokenHash = tokenHash, NewHash = newHash });
        return affected > 0;
    }

    // ── V061-F2: Logout de todos os dispositivos ─────────────────────────────

    public async Task RevokeAllRefreshTokensAsync(string customerId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE customer
            SET refresh_token_hash      = NULL,
                refresh_token_expires_at = NULL,
                updated_at              = NOW()
            WHERE id = @Id
            """,
            new { Id = Guid.Parse(customerId) });
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private static AuthCustomerDto ToDto(CustomerRow r, List<AddressRow>? addresses) =>
        new(r.Id.ToString(), r.Name, r.Email, r.Provider, r.AvatarUrl, r.EmailVerified,
            r.Phone, r.Document,
            addresses?.Select(ToAddressDto).ToList(),
            r.TotpEnabled);

    private static CustomerAddressDto ToAddressDto(AddressRow a) =>
        new(a.Id.ToString(), a.Label ?? "", a.ZipCode, a.Street,
            a.Number, a.Complement, a.Neighborhood, a.City, a.State, a.IsDefault);

    private sealed class CustomerRow
    {
        public Guid    Id            { get; init; }
        public string  Name          { get; init; } = "";
        public string  Email         { get; init; } = "";
        public string  Provider      { get; init; } = "";
        public string? AvatarUrl     { get; init; }
        public bool    EmailVerified { get; init; }
        public string? Phone         { get; init; }
        public string? Document      { get; init; }
        public string? PasswordHash  { get; init; }
        public bool    TotpEnabled   { get; init; }
    }

    private sealed class AddressRow
    {
        public Guid    Id           { get; init; }
        public string? Label        { get; init; }
        public string  ZipCode      { get; init; } = "";
        public string  Street       { get; init; } = "";
        public string? Number       { get; init; }
        public string? Complement   { get; init; }
        public string? Neighborhood { get; init; }
        public string  City         { get; init; } = "";
        public string  State        { get; init; } = "";
        public bool    IsDefault    { get; init; }
    }
}
