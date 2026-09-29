using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Messaging;

public sealed class RabbitMqNotificationPublisher : INotificationPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel    _channel;

    public RabbitMqNotificationPublisher(string hostName, string userName = "guest", string password = "guest")
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel    = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string queue, T notification, CancellationToken ct = default) where T : class
    {
        await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));
        var props = new BasicProperties
        {
            Persistent  = true,
            ContentType = "application/json",
            Type        = typeof(T).Name,
            Timestamp   = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };
        await _channel.BasicPublishAsync("", queue, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
}
