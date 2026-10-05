using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UcpAgent.Infrastructure.Persistence.Repositories;

namespace UcpAgent.Infrastructure.Persistence.Workers;

public sealed class PaymentOutboxWorker : BackgroundService
{
    private readonly OutboxRepository             _outbox;
    private readonly IProducer<string, string>    _producer;
    private readonly ILogger<PaymentOutboxWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    public PaymentOutboxWorker(
        OutboxRepository outbox,
        IProducer<string, string> producer,
        ILogger<PaymentOutboxWorker> logger)
    {
        _outbox   = outbox;
        _producer = producer;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[PaymentOutboxWorker] iniciado");

        using var timer = new PeriodicTimer(Interval);

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                var messages = await _outbox.GetPendingPaymentsAsync(ct: ct);
                if (!messages.Any()) continue;

                _logger.LogInformation("[PaymentOutboxWorker] {Count} mensagens pendentes", messages.Count);

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

                        await _outbox.MarkPaymentSentAsync(msg.Id, ct);
                        _logger.LogInformation("[PaymentOutboxWorker] ✅ {Id} → {Topic}", msg.Id, msg.Topic);
                    }
                    catch (Exception ex)
                    {
                        var backoff = Backoff(msg.RetryCount);
                        await _outbox.MarkPaymentFailedAsync(msg.Id, ex.Message, backoff, ct);
                        _logger.LogWarning("[PaymentOutboxWorker] ❌ {Id} retry #{Retry} em {Backoff}s — {Error}",
                            msg.Id, msg.RetryCount + 1, backoff.TotalSeconds, ex.Message);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PaymentOutboxWorker] erro no loop");
            }
        }

        _logger.LogInformation("[PaymentOutboxWorker] encerrado");
    }

    private static TimeSpan Backoff(int retryCount) =>
        TimeSpan.FromSeconds(Math.Min(300, 30 * Math.Pow(2, retryCount)));
}
