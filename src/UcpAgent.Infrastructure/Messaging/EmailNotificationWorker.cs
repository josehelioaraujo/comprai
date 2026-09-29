using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Resend;
using UcpAgent.SharedKernel.Events;

namespace UcpAgent.Infrastructure.Messaging;

/// <summary>
/// Worker que consome filas RabbitMQ e envia emails via Resend.
/// Quando RESEND_API_KEY não está configurado, loga em modo fake (sem envio real).
///
/// Filas consumidas:
///   notifications.order.confirmation → email "Pedido recebido"
///   notifications.order.status       → email "Status atualizado"
/// </summary>
public sealed class EmailNotificationWorker : BackgroundService
{
    private readonly ILogger<EmailNotificationWorker> _logger;
    private readonly IResend? _resend;
    private readonly string _fromEmail;
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly bool _fakeMode;

    public EmailNotificationWorker(
        ILogger<EmailNotificationWorker> logger,
        IConfiguration config,
        IResend? resend = null)
    {
        _logger    = logger;
        _resend    = resend;
        _fromEmail = config["Resend:FromEmail"] ?? "noreply@comprai.app";
        _hostName  = config["RabbitMq:Host"]     ?? "localhost";
        _userName  = config["RabbitMq:UserName"] ?? "guest";
        _password  = config["RabbitMq:Password"] ?? "guest";
        _fakeMode  = string.IsNullOrEmpty(config["Resend:ApiKey"]);

        if (_fakeMode)
            _logger.LogWarning("[EmailWorker] RESEND_API_KEY não configurado — modo FAKE ativo (sem envio real)");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Aguarda RabbitMQ subir no docker compose
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        IConnection? connection = null;
        IModel?      channel    = null;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName                   = _hostName,
                UserName                   = _userName,
                Password                   = _password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            };
            connection = factory.CreateConnection();
            channel    = connection.CreateModel();

            foreach (var queue in new[] { NotificationQueues.OrderConfirmation, NotificationQueues.OrderStatusUpdate })
                channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);

            channel.BasicQos(0, prefetchCount: 10, global: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, ea) => OnMessage(channel, ea);

            channel.BasicConsume(NotificationQueues.OrderConfirmation, autoAck: false, consumer);
            channel.BasicConsume(NotificationQueues.OrderStatusUpdate,  autoAck: false, consumer);

            _logger.LogInformation("[EmailWorker] Aguardando mensagens — modo: {Mode}",
                _fakeMode ? "FAKE" : "Resend");

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException) { /* shutdown normal */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailWorker] Erro ao conectar no RabbitMQ");
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
            var msgType = ea.BasicProperties?.Type ?? "";

            if (msgType == nameof(OrderConfirmationNotification))
            {
                var n = JsonSerializer.Deserialize<OrderConfirmationNotification>(body);
                if (n is not null) SendOrderConfirmation(n).GetAwaiter().GetResult();
            }
            else if (msgType == nameof(OrderStatusNotification))
            {
                var n = JsonSerializer.Deserialize<OrderStatusNotification>(body);
                if (n is not null) SendOrderStatus(n).GetAwaiter().GetResult();
            }
            else
            {
                _logger.LogWarning("[EmailWorker] Tipo desconhecido: {Type}", msgType);
            }

            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailWorker] Erro ao processar mensagem");
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task SendOrderConfirmation(OrderConfirmationNotification n)
    {
        var subject = $"✅ Pedido {n.OrderId} confirmado!";
        var html = $"""
            <h2>Olá, {n.CustomerName}!</h2>
            <p>Seu pedido foi recebido com sucesso.</p>
            <table>
              <tr><td><b>Pedido:</b></td><td>{n.OrderId}</td></tr>
              <tr><td><b>Itens:</b></td><td>{n.ItemCount}</td></tr>
              <tr><td><b>Total:</b></td><td>R$ {n.Total:F2}</td></tr>
              <tr><td><b>Data:</b></td><td>{n.CreatedAt:dd/MM/yyyy HH:mm}</td></tr>
            </table>
            <p>Acompanhe o status do seu pedido pelo Comprai.</p>
            """;

        await SendEmail(n.CustomerEmail, subject, html);
    }

    private async Task SendOrderStatus(OrderStatusNotification n)
    {
        var subject = $"📦 Pedido {n.OrderId} atualizado: {n.NewStatus}";
        var html = $"""
            <h2>Atualização do seu pedido</h2>
            <p>O status do pedido <b>{n.OrderId}</b> foi atualizado.</p>
            <table>
              <tr><td><b>Status anterior:</b></td><td>{n.OldStatus}</td></tr>
              <tr><td><b>Novo status:</b></td><td><b>{n.NewStatus}</b></td></tr>
              <tr><td><b>Data:</b></td><td>{n.UpdatedAt:dd/MM/yyyy HH:mm}</td></tr>
            </table>
            """;

        await SendEmail(n.CustomerEmail, subject, html);
    }

    private async Task SendEmail(string to, string subject, string html)
    {
        if (_fakeMode || _resend is null)
        {
            _logger.LogInformation("[FAKE EMAIL] Para: {To} | Assunto: {Subject}", to, subject);
            return;
        }

        try
        {
            var message = new EmailMessage
            {
                From        = _fromEmail,
                Subject     = subject,
                HtmlBody    = html,
            };
            message.To.Add(to);
            await _resend.EmailSendAsync(message);
            _logger.LogInformation("[EmailWorker] Email enviado via Resend para {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailWorker] Falha ao enviar email via Resend para {To}", to);
        }
    }
}
