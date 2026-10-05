using System.Reflection;
using Dapper;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence;

public sealed class CompraiDbInitializer
{
    private readonly IDbConnectionFactory _factory;

    public CompraiDbInitializer(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = (System.Data.Common.DbConnection) await _factory.CreateAsync(ct);

        var migrations = GetMigrationScripts();

        foreach (var (name, sql) in migrations)
        {
            try
            {
                await conn.ExecuteAsync(sql);
                Console.WriteLine($"[DB] Migration aplicada: {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Erro na migration {name}: {ex.Message}");
                throw;
            }
        }
    }

    private static IEnumerable<(string Name, string Sql)> GetMigrationScripts()
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
                return (Name: n.Replace(prefix, ""), Sql: reader.ReadToEnd());
            });
    }
}
