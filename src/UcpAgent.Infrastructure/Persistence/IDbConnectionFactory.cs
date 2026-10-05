using System.Data;

namespace UcpAgent.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateAsync(CancellationToken ct = default);
}
