using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Messaging;

/// <summary>
/// Publica notificações no RabbitMQ com conexão lazy e tolerância a falhas.
/// Se o broker não estiver disponível, as publicações são silenciosamente ignoradas.
/// </summary>
public sealed class RabbitMqNotificationPublisher : INotificationPublisher, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel?    _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqNotificationPublisher(string hostName, string userName = "guest", string password = "guest")
    {
        _factory = new ConnectionFactory
        {
            HostName                   = hostName,
            UserName                   = userName,
            Password                   = password,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
        };
    }

    public async Task PublishAsync<T>(string queue, T notification, CancellationToken ct = default) where T : class
    {
        try
        {
            var channel = await GetChannelAsync(ct);
            if (channel is null) return;

            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));
            var props = new BasicProperties
            {
                Persistent  = true,
                ContentType = "application/json",
                Type        = typeof(T).Name,
                Timestamp   = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            };
            await channel.BasicPublishAsync("", queue, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
        }
        catch (Exception)
        {
            // RabbitMQ indisponível — notificação descartada silenciosamente
            _connection = null;
            _channel    = null;
        }
    }

    private async Task<IChannel?> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is not null) return _channel;

        await _lock.WaitAsync(ct);
        try
        {
            if (_channel is not null) return _channel;
            _connection = await _factory.CreateConnectionAsync(ct);
            _channel    = await _connection.CreateChannelAsync(cancellationToken: ct);
            return _channel;
        }
        catch
        {
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)    await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
        _lock.Dispose();
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
}
