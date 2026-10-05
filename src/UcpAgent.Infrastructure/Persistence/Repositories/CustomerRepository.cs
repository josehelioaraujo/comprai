using Dapper;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository
{
    private readonly IDbConnectionFactory _factory;

    public CustomerRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Guid?> GetIdByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT id FROM customer WHERE email = @email", new { email });
    }

    public async Task<Guid> UpsertAsync(string name, string email, string? phone = null,
        string channel = "web", CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        return await conn.QuerySingleAsync<Guid>("""
            INSERT INTO customer (name, email, phone, channel)
            VALUES (@name, @email, @phone, @channel)
            ON CONFLICT (email) DO UPDATE
                SET name       = EXCLUDED.name,
                    phone      = EXCLUDED.phone,
                    updated_at = NOW()
            RETURNING id
            """,
            new { name, email, phone, channel });
    }
}
