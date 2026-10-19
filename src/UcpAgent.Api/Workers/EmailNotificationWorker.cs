using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UcpAgent.Api.Auth;
using UcpAgent.Infrastructure.Persistence.Repositories;
using UcpAgent.SharedKernel.Events;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Workers;

/// <summary>
/// Consome filas RabbitMQ e envia e-mails via IEmailService (Brevo → Resend → Null).
/// Email e nome já chegam na notificação — sem consulta extra ao banco para pedidos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EmailNotificationWorker : BackgroundService
{
    private readonly ILogger<EmailNotificationWorker> _logger;
    private readonly IEmailService _emailService;
    private readonly CustomerAuthRepository? _customerRepo;
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _appBaseUrl;

    public EmailNotificationWorker(
        ILogger<EmailNotificationWorker> logger,
        IConfiguration config,
        IEmailService emailService,
        CustomerAuthRepository? customerRepo = null)
    {
        _logger       = logger;
        _emailService = emailService;
        _customerRepo = customerRepo;
        _hostName     = config["RabbitMq:Host"]     ?? "localhost";
        _userName     = config["RabbitMq:UserName"] ?? "guest";
        _password     = config["RabbitMq:Password"] ?? "guest";
        _appBaseUrl   = (config["AppBaseUrl"] ?? "http://2.25.122.11:3005").TrimEnd('/');
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

            _logger.LogInformation("[EmailWorker] Aguardando mensagens via {Provider}",
                _emailService.GetType().Name);

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

    // ─── Order Confirmation ────────────────────────────────────────────────────

    private async Task SendOrderConfirmation(OrderConfirmationNotification n)
    {
        // Email/nome já vêm na notificação; fallback ao banco para pedidos legados
        var (email, name) = await ResolveContactAsync(n.CustomerEmail, n.CustomerName, n.CustomerId);
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("[EmailWorker] Sem email para pedido {OrderId} — notificação ignorada", n.OrderId);
            return;
        }

        var items        = DeserializeItems(n.ItemsJson);
        var orderUrl     = $"{_appBaseUrl}/orders/{n.OrderId}";
        var trackingUrl  = $"{orderUrl}#rastreio";
        var shortId      = n.OrderId.Length >= 8 ? n.OrderId[^8..].ToUpperInvariant() : n.OrderId.ToUpperInvariant();
        var totalFmt     = n.Total.ToString("C", new System.Globalization.CultureInfo("pt-BR"));
        var dateFmt      = n.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
        var paymentLabel = FormatPaymentMethod(n.PaymentMethod);

        var html = $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#f5f5f5;font-family:Inter,Arial,sans-serif">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;padding:32px 16px">
                <tr><td align="center">
                  <table width="100%" style="max-width:560px;background:#fff;border-radius:16px;overflow:hidden;
                                             box-shadow:0 2px 8px rgba(0,0,0,.08)">

                    <!-- Header -->
                    <tr><td style="background:#7c3aed;padding:32px 40px;text-align:center">
                      <p style="margin:0;font-size:14px;color:#e9d5ff;letter-spacing:.05em;text-transform:uppercase">Comprai</p>
                      <h1 style="margin:8px 0 0;font-size:26px;font-weight:700;color:#fff">✅ Pedido confirmado!</h1>
                    </td></tr>

                    <!-- Body -->
                    <tr><td style="padding:32px 40px">

                      <p style="margin:0 0 24px;font-size:15px;color:#374151">
                        Olá, <strong>{Encode(name)}</strong>! Seu pedido foi recebido e está sendo processado.
                      </p>

                      <!-- Resumo do pedido -->
                      <table width="100%" cellpadding="0" cellspacing="0"
                             style="background:#f9fafb;border-radius:12px;padding:20px;margin-bottom:24px">
                        <tr>
                          <td style="font-size:13px;color:#6b7280;padding-bottom:8px">Número do pedido</td>
                          <td align="right" style="font-size:13px;font-weight:700;color:#111827;
                                                   font-family:monospace;padding-bottom:8px">#{shortId}</td>
                        </tr>
                        <tr>
                          <td style="font-size:13px;color:#6b7280;padding-bottom:8px">Data</td>
                          <td align="right" style="font-size:13px;color:#374151;padding-bottom:8px">{dateFmt}</td>
                        </tr>
                        {(string.IsNullOrEmpty(paymentLabel) ? "" : $"""
                        <tr>
                          <td style="font-size:13px;color:#6b7280;padding-bottom:8px">Forma de pagamento</td>
                          <td align="right" style="font-size:13px;color:#374151;padding-bottom:8px">{Encode(paymentLabel)}</td>
                        </tr>
                        """)}
                      </table>

                      <!-- Itens -->
                      {BuildItemsTable(items)}

                      <!-- Total -->
                      <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:32px">
                        <tr>
                          <td style="font-size:16px;font-weight:700;color:#111827">Total</td>
                          <td align="right" style="font-size:20px;font-weight:700;color:#7c3aed">{Encode(totalFmt)}</td>
                        </tr>
                      </table>

                      <!-- CTAs -->
                      <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:24px">
                        <tr>
                          <td style="padding-right:8px">
                            <a href="{Encode(orderUrl)}"
                               style="display:block;text-align:center;background:#7c3aed;color:#fff;
                                      font-weight:700;padding:14px;border-radius:10px;
                                      text-decoration:none;font-size:14px">
                              Ver detalhes do pedido
                            </a>
                          </td>
                          <td style="padding-left:8px">
                            <a href="{Encode(trackingUrl)}"
                               style="display:block;text-align:center;background:#f3f4f6;color:#374151;
                                      font-weight:600;padding:14px;border-radius:10px;
                                      text-decoration:none;font-size:14px">
                              📦 Rastrear entrega
                            </a>
                          </td>
                        </tr>
                      </table>

                      <p style="margin:0;font-size:13px;color:#9ca3af;text-align:center">
                        Dúvidas? Responda este e-mail ou acesse o app Comprai.
                      </p>
                    </td></tr>

                    <!-- Footer -->
                    <tr><td style="background:#f9fafb;padding:20px 40px;text-align:center;
                                   border-top:1px solid #f3f4f6">
                      <p style="margin:0;font-size:12px;color:#9ca3af">
                        © {n.CreatedAt.Year} Comprai · Este e-mail foi gerado automaticamente.
                      </p>
                    </td></tr>

                  </table>
                </td></tr>
              </table>
            </body></html>
            """;

        await _emailService.SendTransactionalAsync(
            email,
            name,
            $"✅ Pedido #{shortId} confirmado — Comprai",
            html);

        _logger.LogInformation("[EmailWorker] Confirmação enviada para {Email} | pedido {OrderId}",
            MaskEmail(email), n.OrderId);
    }

    // ─── Order Status ──────────────────────────────────────────────────────────

    private async Task SendOrderStatus(OrderStatusNotification n)
    {
        var (email, name) = await ResolveContactAsync(string.Empty, string.Empty, n.CustomerId);
        if (string.IsNullOrEmpty(email)) return;

        var shortId = n.OrderId.Length >= 8 ? n.OrderId[^8..].ToUpperInvariant() : n.OrderId.ToUpperInvariant();
        var subject = $"📦 Pedido #{shortId}: {n.NewStatus}";
        var html    = $"""
            <div style="font-family:Inter,Arial,sans-serif;max-width:560px;margin:auto;padding:2rem">
              <h2 style="color:#1a1a2e;margin:0 0 1rem">Atualização do pedido</h2>
              <p style="color:#374151">Olá, {Encode(name)}!</p>
              <p style="color:#374151">Status do pedido <b>{n.OrderId}</b>:</p>
              <p style="font-size:1.25rem;font-weight:700;color:#7c3aed">{Encode(n.NewStatus)}</p>
              <p style="color:#6b7280;font-size:.875rem">Anterior: {Encode(n.OldStatus)}</p>
            </div>
            """;
        await _emailService.SendTransactionalAsync(email, name, subject, html);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Usa email/nome da notificação quando disponíveis; senão busca no banco pelo CustomerId.
    /// </summary>
    private async Task<(string Email, string Name)> ResolveContactAsync(
        string notifEmail, string notifName, string customerId)
    {
        if (!string.IsNullOrEmpty(notifEmail))
            return (notifEmail, string.IsNullOrEmpty(notifName) ? "Cliente" : notifName);

        if (string.IsNullOrEmpty(customerId) || _customerRepo is null)
        {
            _logger.LogWarning("[EmailWorker] Sem email e sem CustomerId — notificação ignorada");
            return (string.Empty, string.Empty);
        }

        var dto = await _customerRepo.GetByIdAsync(customerId);
        if (dto is null)
        {
            _logger.LogWarning("[EmailWorker] Cliente {CustomerId} não encontrado", customerId);
            return (string.Empty, string.Empty);
        }
        return (dto.Email, dto.Name);
    }

    private static IReadOnlyList<CartItemDto> DeserializeItems(string json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<CartItemDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? [];
        }
        catch { return []; }
    }

    private static string BuildItemsTable(IReadOnlyList<CartItemDto> items)
    {
        if (items.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("""
            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:24px;border-collapse:collapse">
              <thead>
                <tr style="border-bottom:2px solid #e5e7eb">
                  <th align="left" style="font-size:12px;color:#6b7280;text-transform:uppercase;
                                          letter-spacing:.05em;padding-bottom:10px;font-weight:600">Produto</th>
                  <th align="center" style="font-size:12px;color:#6b7280;text-transform:uppercase;
                                            letter-spacing:.05em;padding-bottom:10px;font-weight:600">Qtd</th>
                  <th align="right" style="font-size:12px;color:#6b7280;text-transform:uppercase;
                                           letter-spacing:.05em;padding-bottom:10px;font-weight:600">Subtotal</th>
                </tr>
              </thead>
              <tbody>
            """);

        var culture = new System.Globalization.CultureInfo("pt-BR");
        foreach (var item in items)
        {
            var title    = Encode(item.Product?.Title ?? "Produto");
            var subtotal = item.Subtotal.ToString("C", culture);
            sb.Append($"""
                <tr style="border-bottom:1px solid #f3f4f6">
                  <td style="font-size:14px;color:#111827;padding:12px 0">{title}</td>
                  <td align="center" style="font-size:14px;color:#374151;padding:12px 8px">{item.Quantity}×</td>
                  <td align="right" style="font-size:14px;font-weight:600;color:#111827;padding:12px 0">{Encode(subtotal)}</td>
                </tr>
                """);
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string FormatPaymentMethod(string? raw) => raw?.ToLowerInvariant() switch
    {
        "pix"           => "PIX",
        "credit_card"   => "Cartão de crédito",
        "debit_card"    => "Cartão de débito",
        "boleto"        => "Boleto bancário",
        _ when !string.IsNullOrEmpty(raw) => raw,
        _               => string.Empty,
    };

    private static string Encode(string s) => WebUtility.HtmlEncode(s);

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        return $"{email[..Math.Min(2, at)]}***{email[at..]}";
    }
}
