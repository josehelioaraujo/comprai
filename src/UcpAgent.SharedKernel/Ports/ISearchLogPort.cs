namespace UcpAgent.SharedKernel.Ports;

public sealed record SearchLogEntry(
    string   Query,
    int      ResultCount,
    string   Sources,
    int      DurationMs,
    string?  SessionId,
    DateTime OccurredAt);

public interface ISearchLogPort
{
    Task LogAsync(SearchLogEntry entry, CancellationToken ct = default);
}