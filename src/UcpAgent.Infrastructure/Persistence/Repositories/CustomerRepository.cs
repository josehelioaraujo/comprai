using Dapper;
using UcpAgent.Infrastructure.Persistence;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly IEncryptionService   _enc;

    public CustomerRepository(IDbConnectionFactory factory, IEncryptionService enc)
    {
        _factory = factory;
        _enc     = enc;
    }

    public Task<Guid?> GetIdByEmailAsync(string email, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await _factory.CreateAsync(token);
            return await conn.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT id FROM customer WHERE email = @email", new { email });
        }, ct);

    public Task<Guid> UpsertAsync(string name, string email, string? phone = null,
        string channel = "web", CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await _factory.CreateAsync(token);
            return await conn.QuerySingleAsync<Guid>("""
                INSERT INTO customer (name, email, phone, channel)
                VALUES (@name, @email, @phone, @channel)
                ON CONFLICT (email) DO UPDATE
                    SET name       = EXCLUDED.name,
                        phone      = EXCLUDED.phone,
                        updated_at = NOW()
                RETURNING id
                """,
                new { name, email, phone = _enc.Encrypt(phone), channel });
        }, ct);
}
