using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UcpAgent.Infrastructure.Persistence.Repositories;

namespace UcpAgent.Infrastructure.Persistence.Workers;

public sealed class OrderOutboxWorker : BackgroundService
{
    private readonly OutboxRepository          _outbox;
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<OrderOutboxWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    public OrderOutboxWorker(
        OutboxRepository outbox,
        IProducer<string, string> producer,
        ILogger<OrderOutboxWorker> logger)
    {
        _outbox   = outbox;
        _producer = producer;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[OrderOutboxWorker] iniciado");

        using var timer = new PeriodicTimer(Interval);

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                var messages = await _outbox.GetPendingOrdersAsync(ct: ct);
                if (!messages.Any()) continue;

                _logger.LogInformation("[OrderOutboxWorker] {Count} mensagens pendentes", messages.Count);

                foreach (var msg in messages)
                {
                    try
                    {
                        await _producer.ProduceAsync(msg.Topic,
                            new Message<string, string>
                            {
                                Key   = msg.Id.ToString(),
                                Value = msg.Payload
                            }, ct);

                        await _outbox.MarkOrderSentAsync(msg.Id, ct);
                        _logger.LogInformation("[OrderOutboxWorker] ✅ {Id} → {Topic}", msg.Id, msg.Topic);
                    }
                    catch (Exception ex)
                    {
                        var backoff = Backoff(msg.RetryCount);
                        await _outbox.MarkOrderFailedAsync(msg.Id, ex.Message, backoff, ct);
                        _logger.LogWarning("[OrderOutboxWorker] ❌ {Id} retry #{Retry} em {Backoff}s — {Error}",
                            msg.Id, msg.RetryCount + 1, backoff.TotalSeconds, ex.Message);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OrderOutboxWorker] erro no loop");
            }
        }

        _logger.LogInformation("[OrderOutboxWorker] encerrado");
    }

    private static TimeSpan Backoff(int retryCount) =>
        TimeSpan.FromSeconds(Math.Min(300, 30 * Math.Pow(2, retryCount)));
}
