namespace UcpAgent.SharedKernel.Ports;

/// <summary>
/// Porta de notificações ao usuário (email, push, SMS).
/// Separada de IEventPublisher — RabbitMQ publica aqui; Kafka publica em IEventPublisher.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync<T>(string queue, T notification, CancellationToken ct = default) where T : class;
}
