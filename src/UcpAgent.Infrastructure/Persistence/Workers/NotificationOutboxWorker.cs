using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using UcpAgent.Infrastructure.Persistence.Repositories;

namespace UcpAgent.Infrastructure.Persistence.Workers;

public sealed class NotificationOutboxWorker : BackgroundService
{
    private readonly OutboxRepository                  _outbox;
    private readonly IConnection                       _rabbitConnection;
    private readonly ILogger<NotificationOutboxWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    public NotificationOutboxWorker(
        OutboxRepository outbox,
        IConnection rabbitConnection,
        ILogger<NotificationOutboxWorker> logger)
    {
        _outbox           = outbox;
        _rabbitConnection = rabbitConnection;
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[NotificationOutboxWorker] iniciado");

        using var timer = new PeriodicTimer(Interval);

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                var messages = await _outbox.GetPendingNotificationsAsync(ct: ct);
                if (!messages.Any()) continue;

                _logger.LogInformation("[NotificationOutboxWorker] {Count} mensagens pendentes", messages.Count);

                using var channel = await _rabbitConnection.CreateChannelAsync(cancellationToken: ct);

                foreach (var msg in messages)
                {
                    try
                    {
                        await channel.QueueDeclareAsync(
                            queue:      msg.Topic,
                            durable:    true,
                            exclusive:  false,
                            autoDelete: false,
                            cancellationToken: ct);

                        var body = Encoding.UTF8.GetBytes(msg.Payload);

                        await channel.BasicPublishAsync(
                            exchange:   string.Empty,
                            routingKey: msg.Topic,
                            body:       body,
                            cancellationToken: ct);

                        await _outbox.MarkNotificationSentAsync(msg.Id, ct);
                        _logger.LogInformation("[NotificationOutboxWorker] ✅ {Id} → {Queue}", msg.Id, msg.Topic);
                    }
                    catch (Exception ex)
                    {
                        var backoff = Backoff(msg.RetryCount);
                        await _outbox.MarkNotificationFailedAsync(msg.Id, ex.Message, backoff, ct);
                        _logger.LogWarning("[NotificationOutboxWorker] ❌ {Id} retry #{Retry} em {Backoff}s — {Error}",
                            msg.Id, msg.RetryCount + 1, backoff.TotalSeconds, ex.Message);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NotificationOutboxWorker] erro no loop");
            }
        }

        _logger.LogInformation("[NotificationOutboxWorker] encerrado");
    }

    private static TimeSpan Backoff(int retryCount) =>
        TimeSpan.FromSeconds(Math.Min(300, 30 * Math.Pow(2, retryCount)));
}
