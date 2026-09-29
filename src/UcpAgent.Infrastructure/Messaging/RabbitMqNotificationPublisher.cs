using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Messaging;

/// <summary>
/// Publica notificações ao usuário via RabbitMQ (filas diretas, durable).
/// Separado do KafkaEventPublisher — cada broker tem sua responsabilidade.
/// </summary>
public sealed class RabbitMqNotificationPublisher : INotificationPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqNotificationPublisher(string hostName, string userName = "guest", string password = "guest")
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
        };
        _connection = factory.CreateConnection();
        _channel    = _connection.CreateModel();
    }

    public Task PublishAsync<T>(string queue, T notification, CancellationToken ct = default) where T : class
    {
        // Fila durable — sobrevive a restart do RabbitMQ
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
        var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));
        var props = _channel.CreateBasicProperties();
        props.Persistent    = true;
        props.ContentType   = "application/json";
        props.Type          = typeof(T).Name;
        props.Timestamp     = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        _channel.BasicPublish("", queue, props, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
