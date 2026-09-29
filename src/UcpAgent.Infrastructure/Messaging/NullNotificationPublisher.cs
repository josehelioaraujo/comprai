using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Messaging;

/// <summary>
/// Descarta notificações silenciosamente — usado quando RabbitMQ está desligado.
/// </summary>
public sealed class NullNotificationPublisher : INotificationPublisher
{
    public Task PublishAsync<T>(string queue, T notification, CancellationToken ct = default) where T : class
        => Task.CompletedTask;
}
