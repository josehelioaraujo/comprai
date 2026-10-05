using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace UcpAgent.Infrastructure.Persistence;

public sealed class CompraiDbInitializer
{
    private readonly string _connectionString;

    public CompraiDbInitializer(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public void Initialize()
    {
        EnsureDatabase.For.PostgresqlDatabase(_connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.Contains("Migrations"))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        if (!upgrader.IsUpgradeRequired())
        {
            Console.WriteLine("[DB] Nenhuma migration pendente.");
            return;
        }

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.WriteLine($"[DB] ❌ Falha na migration: {result.Error.Message}");
            throw new InvalidOperationException(
                $"Falha ao aplicar migrations: {result.Error.Message}", result.Error);
        }

        Console.WriteLine("[DB] ✅ Migrations aplicadas com sucesso.");
    }
}
