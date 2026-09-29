using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Messaging;

public sealed class KafkaEventPublisher_Placeholder { } // mantém namespace

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel    _channel;

    public RabbitMqEventPublisher(string hostName, string userName = "guest", string password = "guest")
    {
        var factory = new ConnectionFactory { HostName = hostName, UserName = userName, Password = password };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel    = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string topic, T payload, CancellationToken ct = default) where T : class
    {
        await _channel.ExchangeDeclareAsync(topic, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var props = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };
        await _channel.BasicPublishAsync(topic, string.Empty, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
}
