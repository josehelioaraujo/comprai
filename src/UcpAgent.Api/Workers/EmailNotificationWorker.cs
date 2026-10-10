using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Resend;
using UcpAgent.Infrastructure.Persistence.Repositories;
using UcpAgent.SharedKernel.Events;

namespace UcpAgent.Api.Workers;

/// <summary>
/// Consome filas RabbitMQ (v7) e envia emails via Resend.
/// Sem RESEND_API_KEY opera em modo fake (log only).
/// Email/nome são buscados no banco pelo CustomerId — nunca transitam pela fila.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EmailNotificationWorker : BackgroundService
{
    private readonly ILogger<EmailNotificationWorker> _logger;
    private readonly IResend? _resend;
    private readonly CustomerAuthRepository? _customerRepo;
    private readonly string _fromEmail;
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly bool _fakeMode;

    public EmailNotificationWorker(
        ILogger<EmailNotificationWorker> logger,
        IConfiguration config,
        IResend? resend = null,
        CustomerAuthRepository? customerRepo = null)
    {
        _logger       = logger;
        _resend       = resend;
        _customerRepo = customerRepo;
        _fromEmail    = config["Resend:FromEmail"] ?? "noreply@comprai.app";
        _hostName     = config["RabbitMq:Host"]     ?? "localhost";
        _userName     = config["RabbitMq:UserName"] ?? "guest";
        _password     = config["RabbitMq:Password"] ?? "guest";
        _fakeMode     = string.IsNullOrEmpty(config["Resend:ApiKey"]
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
        var customer = await ResolveCustomerAsync(n.CustomerId);
        if (customer is null) return;
        var (email, name) = customer.Value;

        var subject = $"✅ Pedido {n.OrderId} confirmado!";
        var html    = $"<h2>Olá, {name}!</h2><p>Pedido: <b>{n.OrderId}</b> | Total: R$ {n.Total:F2} | Itens: {n.ItemCount}</p>";
        await SendEmail(email, subject, html);
    }

    private async Task SendOrderStatus(OrderStatusNotification n)
    {
        var customer = await ResolveCustomerAsync(n.CustomerId);
        if (customer is null) return;
        var (email, _) = customer.Value;

        var subject = $"📦 Pedido {n.OrderId}: {n.NewStatus}";
        var html    = $"<h2>Pedido {n.OrderId}</h2><p>Status: <b>{n.OldStatus}</b> → <b>{n.NewStatus}</b></p>";
        await SendEmail(email, subject, html);
    }

    /// <summary>
    /// Busca email e nome do cliente no banco pelo CustomerId.
    /// Retorna null e loga warning se CustomerId vazio ou repositório indisponível.
    /// </summary>
    private async Task<(string Email, string Name)?> ResolveCustomerAsync(string customerId)
    {
        if (string.IsNullOrEmpty(customerId))
        {
            _logger.LogWarning("[EmailWorker] CustomerId vazio — notificação ignorada");
            return null;
        }
        if (_customerRepo is null)
        {
            _logger.LogWarning("[EmailWorker] CustomerAuthRepository não disponível — notificação ignorada");
            return null;
        }
        var dto = await _customerRepo.GetByIdAsync(customerId);
        if (dto is null)
        {
            _logger.LogWarning("[EmailWorker] Cliente {CustomerId} não encontrado", customerId);
            return null;
        }
        return (dto.Email, dto.Name);
    }

    private async Task SendEmail(string to, string subject, string html)
    {
        if (_fakeMode || _resend is null)
        {
            _logger.LogInformation("[FAKE EMAIL] Para: {To} | {Subject}", MaskEmail(to), subject);
            return;
        }
        try
        {
            var msg = new EmailMessage { From = _fromEmail, Subject = subject, HtmlBody = html };
            msg.To.Add(to);
            await _resend.EmailSendAsync(msg);
        }
        catch (Exception ex) { _logger.LogError(ex, "[EmailWorker] Falha Resend para {To}", MaskEmail(to)); }
    }

    /// <summary>Mascara email nos logs: "ab***@domain.com"</summary>
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local  = email[..Math.Min(2, at)];
        var domain = email[at..];
        return $"{local}***{domain}";
    }
}
