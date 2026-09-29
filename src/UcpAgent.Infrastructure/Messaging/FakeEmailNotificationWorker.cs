using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UcpAgent.SharedKernel.Events;

namespace UcpAgent.Infrastructure.Messaging;

/// <summary>
/// Worker que consome filas de notificação do RabbitMQ e simula envio de email.
///
/// FAKE — estrutura pronta para integração com Resend:
///   1. Substituir o bloco "fake" por: await _resend.Emails.SendAsync(...)
///   2. Injetar ResendClient no construtor
///   3. Remover [ExcludeFromCodeCoverage]
///
/// Filas consumidas:
///   notifications.order.confirmation → email "Pedido recebido"
///   notifications.order.status       → email "Status atualizado"
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Fake — substituir por Resend na V042+")]
public sealed class FakeEmailNotificationWorker : BackgroundService
{
    private readonly ILogger<FakeEmailNotificationWorker> _logger;
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;

    public FakeEmailNotificationWorker(
        ILogger<FakeEmailNotificationWorker> logger,
        IConfiguration config)
    {
        _logger   = logger;
        _hostName = config["RabbitMq:Host"]     ?? "localhost";
        _userName = config["RabbitMq:UserName"] ?? "guest";
        _password = config["RabbitMq:Password"] ?? "guest";
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Aguarda RabbitMQ subir (especialmente no docker compose)
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        IConnection? connection = null;
        IModel?      channel    = null;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = _userName,
                Password = _password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            };
            connection = factory.CreateConnection();
            channel    = connection.CreateModel();

            // Declarar as filas que vamos consumir
            foreach (var queue in new[] { NotificationQueues.OrderConfirmation, NotificationQueues.OrderStatusUpdate })
                channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);

            channel.BasicQos(0, prefetchCount: 10, global: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, ea) => OnMessage(channel, ea);

            channel.BasicConsume(NotificationQueues.OrderConfirmation, autoAck: false, consumer);
            channel.BasicConsume(NotificationQueues.OrderStatusUpdate,  autoAck: false, consumer);

            _logger.LogInformation("[NotificationWorker] Aguardando mensagens em {Queues}",
                string.Join(", ", NotificationQueues.OrderConfirmation, NotificationQueues.OrderStatusUpdate));

            // Mantém o worker vivo
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException) { /* shutdown normal */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NotificationWorker] Erro ao conectar no RabbitMQ");
        }
        finally
        {
            channel?.Dispose();
            connection?.Dispose();
        }
    }

    private void OnMessage(IModel channel, BasicDeliverEventArgs ea)
    {
        try
        {
            var body    = Encoding.UTF8.GetString(ea.Body.Span);
            var msgType = ea.BasicProperties?.Type ?? "unknown";

            // ── FAKE: substituir este bloco por chamada ao Resend ─────────────
            if (msgType == nameof(OrderConfirmationNotification))
            {
                var n = JsonSerializer.Deserialize<OrderConfirmationNotification>(body);
                if (n is not null)
                    _logger.LogInformation(
                        "[FAKE EMAIL] Para: {Email} | Assunto: Pedido {OrderId} confirmado! | Total: R$ {Total:F2}",
                        n.CustomerEmail, n.OrderId, n.Total);
            }
            else if (msgType == nameof(OrderStatusNotification))
            {
                var n = JsonSerializer.Deserialize<OrderStatusNotification>(body);
                if (n is not null)
                    _logger.LogInformation(
                        "[FAKE EMAIL] Para: {Email} | Assunto: Pedido {OrderId} — status: {Old} → {New}",
                        n.CustomerEmail, n.OrderId, n.OldStatus, n.NewStatus);
            }
            else
            {
                _logger.LogWarning("[NotificationWorker] Mensagem desconhecida: {Type}", msgType);
            }
            // ── FIM DO BLOCO FAKE ─────────────────────────────────────────────

            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NotificationWorker] Erro ao processar mensagem");
            // Rejeita sem requeue — evita loop infinito em mensagem corrompida
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }
}
