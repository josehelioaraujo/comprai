using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventPublisher(string hostName, string userName = "guest", string password = "guest")
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };
        _connection = factory.CreateConnection();
        _channel    = _connection.CreateModel();
    }

    public Task PublishAsync<T>(string topic, T payload, CancellationToken ct = default) where T : class
    {
        _channel.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);
        var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        _channel.BasicPublish(topic, string.Empty, props, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
