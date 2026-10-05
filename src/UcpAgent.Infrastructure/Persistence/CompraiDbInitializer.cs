using System.Reflection;
using Dapper;
using Npgsql;

namespace UcpAgent.Infrastructure.Persistence;

public sealed class CompraiDbInitializer
{
    private readonly IDbConnectionFactory _factory;

    private const string CreateSchemaVersion = """
        CREATE TABLE IF NOT EXISTS schema_version (
            version    VARCHAR(100) PRIMARY KEY,
            applied_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
            checksum   VARCHAR(64)
        );
        """;

    public CompraiDbInitializer(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection) await _factory.CreateAsync(ct);

        // Garante tabela de controle
        await conn.ExecuteAsync(CreateSchemaVersion);

        var applied = (await conn.QueryAsync<string>(
            "SELECT version FROM schema_version ORDER BY version"))
            .ToHashSet();

        var pending = GetMigrationScripts()
            .Where(m => !applied.Contains(m.Name))
            .ToList();

        if (pending.Count == 0)
        {
            Console.WriteLine("[DB] Nenhuma migration pendente.");
            return;
        }

        foreach (var (name, sql, checksum) in pending)
        {
            await using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                await conn.ExecuteAsync(sql, transaction: tx);
                await conn.ExecuteAsync(
                    "INSERT INTO schema_version (version, checksum) VALUES (@version, @checksum)",
                    new { version = name, checksum },
                    transaction: tx);

                await tx.CommitAsync(ct);
                Console.WriteLine($"[DB] ✅ Migration aplicada: {name}");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                Console.WriteLine($"[DB] ❌ Erro na migration {name}: {ex.Message}");
                throw;
            }
        }
    }

    private static IEnumerable<(string Name, string Sql, string Checksum)> GetMigrationScripts()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix   = "UcpAgent.Infrastructure.Persistence.Migrations.";

        return assembly
            .GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix) && n.EndsWith(".sql"))
            .OrderBy(n => n)
            .Select(n =>
            {
                using var stream = assembly.GetManifestResourceStream(n)!;
                using var reader = new StreamReader(stream);
                var sql      = reader.ReadToEnd();
                var checksum = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(sql)))[..16];
                var name = n.Replace(prefix, "").Replace(".sql", "");
                return (name, sql, checksum);
            });
    }
}
