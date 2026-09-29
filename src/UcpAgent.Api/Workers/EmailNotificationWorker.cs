using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Resend;
using UcpAgent.SharedKernel.Events;

namespace UcpAgent.Api.Workers;

/// <summary>
/// Consome filas RabbitMQ (v7) e envia emails via Resend.
/// Sem RESEND_API_KEY opera em modo fake (log only).
/// </summary>
[ExcludeFromCodeCoverage]
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
        _fakeMode  = string.IsNullOrEmpty(config["Resend:ApiKey"]
                     ?? Environment.GetEnvironmentVariable("RESEND_API_KEY"));

        if (_fakeMode)
            _logger.LogWarning("[EmailWorker] RESEND_API_KEY não configurado — modo FAKE");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        IConnection? connection = null;
        IChannel?    channel    = null;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName                   = _hostName,
                UserName                   = _userName,
                Password                   = _password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            };
            connection = await factory.CreateConnectionAsync(ct);
            channel    = await connection.CreateChannelAsync(cancellationToken: ct);

            foreach (var queue in new[] { NotificationQueues.OrderConfirmation, NotificationQueues.OrderStatusUpdate })
                await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

            await channel.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) => OnMessageAsync(channel, ea, ct);

            await channel.BasicConsumeAsync(NotificationQueues.OrderConfirmation, autoAck: false, consumer, ct);
            await channel.BasicConsumeAsync(NotificationQueues.OrderStatusUpdate,  autoAck: false, consumer, ct);

            _logger.LogInformation("[EmailWorker] Aguardando mensagens — modo: {Mode}",
                _fakeMode ? "FAKE" : "Resend");

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.LogError(ex, "[EmailWorker] Erro RabbitMQ"); }
        finally
        {
            if (channel is not null) await channel.CloseAsync();
            if (connection is not null) await connection.CloseAsync();
        }
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        try
        {
            var body    = Encoding.UTF8.GetString(ea.Body.Span);
            var msgType = ea.BasicProperties?.Type ?? "";

            if (msgType == nameof(OrderConfirmationNotification))
            {
                var n = JsonSerializer.Deserialize<OrderConfirmationNotification>(body);
                if (n is not null) await SendOrderConfirmation(n);
            }
            else if (msgType == nameof(OrderStatusNotification))
            {
                var n = JsonSerializer.Deserialize<OrderStatusNotification>(body);
                if (n is not null) await SendOrderStatus(n);
            }
            else
            {
                _logger.LogWarning("[EmailWorker] Tipo desconhecido: {Type}", msgType);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailWorker] Erro ao processar mensagem");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }

    private async Task SendOrderConfirmation(OrderConfirmationNotification n)
    {
        var subject = $"✅ Pedido {n.OrderId} confirmado!";
        var html = $"<h2>Olá, {n.CustomerName}!</h2><p>Pedido: <b>{n.OrderId}</b> | Total: R$ {n.Total:F2} | Itens: {n.ItemCount}</p>";
        await SendEmail(n.CustomerEmail, subject, html);
    }

    private async Task SendOrderStatus(OrderStatusNotification n)
    {
        var subject = $"📦 Pedido {n.OrderId}: {n.NewStatus}";
        var html = $"<h2>Pedido {n.OrderId}</h2><p>Status: <b>{n.OldStatus}</b> → <b>{n.NewStatus}</b></p>";
        await SendEmail(n.CustomerEmail, subject, html);
    }

    private async Task SendEmail(string to, string subject, string html)
    {
        if (_fakeMode || _resend is null)
        {
            _logger.LogInformation("[FAKE EMAIL] Para: {To} | {Subject}", to, subject);
            return;
        }
        try
        {
            var msg = new EmailMessage { From = _fromEmail, Subject = subject, HtmlBody = html };
            msg.To.Add(to);
            await _resend.EmailSendAsync(msg);
        }
        catch (Exception ex) { _logger.LogError(ex, "[EmailWorker] Falha Resend para {To}", to); }
    }
}
