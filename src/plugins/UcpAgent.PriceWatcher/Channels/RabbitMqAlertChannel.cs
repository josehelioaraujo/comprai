using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using UcpAgent.PriceWatcher.Models;

namespace UcpAgent.PriceWatcher.Channels;

public sealed class RabbitMqAlertChannel : IPriceAlertChannel, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel    _channel;
    private readonly ILogger<RabbitMqAlertChannel> _logger;
    private const string Exchange = "price.alert";

    public RabbitMqAlertChannel(string host, string user, string password,
        ILogger<RabbitMqAlertChannel> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory { HostName = host, UserName = user, Password = password };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel    = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(PriceAlertMessage alert, string? email, CancellationToken ct = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(alert));
        var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
        await _channel.BasicPublishAsync(Exchange, routingKey: "", mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
        _logger.LogInformation("Alerta publicado no RabbitMQ exchange={Exchange}", Exchange);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
