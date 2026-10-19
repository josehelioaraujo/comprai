using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mail;
using Resend;

namespace UcpAgent.Api.Auth;

/// <summary>Abstração de envio de e-mail transacional.</summary>
public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string toName, string code, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default);
    Task SendAdminInviteEmailAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default);
    /// <summary>Envia e-mail transacional genérico com HTML pronto.</summary>
    Task SendTransactionalAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>
/// Implementação real via Resend SDK.
/// Registrada somente quando RESEND_API_KEY está configurado.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResendEmailService(IResend resend, IConfiguration config) : IEmailService
{
    private readonly string _from = config["Resend:From"] ?? "noreply@comprai.app";

    public async Task SendVerificationEmailAsync(string toEmail, string toName, string code, CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Inter,sans-serif;max-width:480px;margin:auto;padding:2rem">
              <h2 style="color:#1a1a2e;margin:0 0 1rem">Verificação de e-mail</h2>
              <p style="color:#374151;margin:0 0 .5rem">Olá, {Encode(toName)}!</p>
              <p style="color:#374151;margin:0 0 1.5rem">
                Use o código abaixo para confirmar seu endereço de e-mail no Comprai.
              </p>
              <div style="font-size:2.25rem;font-weight:700;letter-spacing:.6rem;
                          background:#f3f4f6;padding:1.25rem;border-radius:12px;
                          text-align:center;color:#111827;font-family:monospace">
                {code}
              </div>
              <p style="color:#9ca3af;font-size:.8125rem;margin:1.25rem 0 0">
                Válido por 15 minutos. Não compartilhe este código.
              </p>
            </div>
            """;

        var msg = new EmailMessage
        {
            From     = _from,
            Subject  = $"Seu código de verificação Comprai: {code}",
            HtmlBody = html,
        };
        msg.To.Add(toEmail);
        await resend.EmailSendAsync(msg, ct);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Inter,sans-serif;max-width:480px;margin:auto;padding:2rem">
              <h2 style="color:#1a1a2e;margin:0 0 1rem">Redefinição de senha</h2>
              <p style="color:#374151;margin:0 0 .5rem">Olá, {Encode(toName)}!</p>
              <p style="color:#374151;margin:0 0 1.5rem">
                Recebemos uma solicitação para redefinir a senha da sua conta Comprai.
                Clique no botão abaixo para criar uma nova senha.
              </p>
              <a href="{System.Net.WebUtility.HtmlEncode(resetLink)}"
                 style="display:inline-block;background:#7c3aed;color:#fff;font-weight:700;
                        padding:.75rem 1.75rem;border-radius:8px;text-decoration:none;font-size:15px">
                Redefinir senha
              </a>
              <p style="color:#9ca3af;font-size:.8125rem;margin:1.25rem 0 0">
                Link válido por 1 hora. Se você não solicitou isso, ignore este e-mail.
              </p>
              <p style="color:#d1d5db;font-size:.75rem;margin:.5rem 0 0">
                Ou copie e cole no navegador: {System.Net.WebUtility.HtmlEncode(resetLink)}
              </p>
            </div>
            """;

        var msg = new EmailMessage
        {
            From     = _from,
            Subject  = "Redefinição de senha — Comprai",
            HtmlBody = html,
        };
        msg.To.Add(toEmail);
        await resend.EmailSendAsync(msg, ct);
    }

    public async Task SendAdminInviteEmailAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Inter,sans-serif;max-width:480px;margin:auto;padding:2rem">
              <div style="text-align:center;margin-bottom:1.5rem">
                <div style="display:inline-flex;align-items:center;justify-content:center;
                            width:56px;height:56px;border-radius:16px;background:#7c3aed">
                  <svg width="28" height="28" fill="none" stroke="#fff" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955
                         11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29
                         9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                  </svg>
                </div>
              </div>
              <h2 style="color:#1a1a2e;margin:0 0 1rem;text-align:center">Bem-vindo ao Comprai Admin</h2>
              <p style="color:#374151;margin:0 0 .5rem">Olá, {Encode(toName)}!</p>
              <p style="color:#374151;margin:0 0 1.5rem">
                Sua conta de administrador foi criada. Clique no botão abaixo para confirmar
                seu e-mail e ativar o acesso ao painel administrativo.
              </p>
              <div style="text-align:center;margin:2rem 0">
                <a href="{System.Net.WebUtility.HtmlEncode(confirmLink)}"
                   style="display:inline-block;background:#7c3aed;color:#fff;font-weight:700;
                          padding:.875rem 2rem;border-radius:10px;text-decoration:none;
                          font-size:15px;letter-spacing:.01em">
                  ✓ Confirmar cadastro
                </a>
              </div>
              <p style="color:#9ca3af;font-size:.8125rem;margin:1.25rem 0 0;text-align:center">
                Link válido por 24 horas. Se você não solicitou este acesso, ignore este e-mail.
              </p>
              <p style="color:#d1d5db;font-size:.75rem;margin:.5rem 0 0;text-align:center">
                Ou copie e cole no navegador:<br/>
                <span style="color:#6b7280">{System.Net.WebUtility.HtmlEncode(confirmLink)}</span>
              </p>
            </div>
            """;

        var msg = new EmailMessage
        {
            From     = _from,
            Subject  = "Confirme seu cadastro — Comprai Admin",
            HtmlBody = html,
        };
        msg.To.Add(toEmail);
        await resend.EmailSendAsync(msg, ct);
    }

    public async Task SendTransactionalAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var msg = new EmailMessage { From = _from, Subject = subject, HtmlBody = htmlBody };
        msg.To.Add(toEmail);
        await resend.EmailSendAsync(msg, ct);
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}

/// <summary>
/// Implementação via Brevo SMTP relay.
/// Registrada quando Brevo:ApiKey está configurado.
/// Envia para qualquer destinatário sem necessidade de domínio verificado.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BrevoEmailService(IConfiguration config, ILogger<BrevoEmailService> logger) : IEmailService
{
    private const string SmtpHost = "smtp-relay.brevo.com";
    private const int    SmtpPort = 587;

    private readonly string _login  = config["Brevo:Login"]  ?? string.Empty;
    private readonly string _apiKey = config["Brevo:ApiKey"] ?? string.Empty;
    private readonly string _from   = config["Brevo:From"]   ?? "helioaraujoandrade@outlook.com";

    private async Task SendAsync(string toEmail, string subject, string html, CancellationToken ct)
    {
        var loginOk  = !string.IsNullOrEmpty(_login);
        var apiKeyOk = !string.IsNullOrEmpty(_apiKey);
        logger.LogInformation(
            "[Brevo] Enviando e-mail | To={To} | From={From} | Subject={Subject} | Login={LoginOk} | ApiKey={ApiKeyOk}",
            toEmail, _from, subject, loginOk, apiKeyOk);

        if (!loginOk || !apiKeyOk)
        {
            logger.LogError("[Brevo] Credenciais ausentes — Brevo:Login={LoginOk} Brevo:ApiKey={ApiKeyOk}. E-mail não enviado.", loginOk, apiKeyOk);
            return;
        }

        try
        {
            using var client = new SmtpClient(SmtpHost, SmtpPort)
            {
                Credentials = new NetworkCredential(_login, _apiKey),
                EnableSsl   = true,
            };
            using var msg = new MailMessage(_from, toEmail, subject, html) { IsBodyHtml = true };
            await client.SendMailAsync(msg, ct);
            logger.LogInformation("[Brevo] E-mail enviado com sucesso | To={To}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Brevo] Falha SMTP | Host={Host}:{Port} | To={To}", SmtpHost, SmtpPort, toEmail);
            throw;
        }
    }

    public Task SendVerificationEmailAsync(string toEmail, string toName, string code, CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Inter,sans-serif;max-width:480px;margin:auto;padding:2rem">
              <h2 style="color:#1a1a2e;margin:0 0 1rem">Verificação de e-mail</h2>
              <p style="color:#374151;margin:0 0 .5rem">Olá, {Encode(toName)}!</p>
              <p style="color:#374151;margin:0 0 1.5rem">
                Use o código abaixo para confirmar seu endereço de e-mail no Comprai.
              </p>
              <div style="font-size:2.25rem;font-weight:700;letter-spacing:.6rem;
                          background:#f3f4f6;padding:1.25rem;border-radius:12px;
                          text-align:center;color:#111827;font-family:monospace">
                {code}
              </div>
              <p style="color:#9ca3af;font-size:.8125rem;margin:1.25rem 0 0">
                Válido por 15 minutos. Não compartilhe este código.
              </p>
            </div>
            """;
        return SendAsync(toEmail, $"Seu código de verificação Comprai: {code}", html, ct);
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Inter,sans-serif;max-width:480px;margin:auto;padding:2rem">
              <h2 style="color:#1a1a2e;margin:0 0 1rem">Redefinição de senha</h2>
              <p style="color:#374151;margin:0 0 .5rem">Olá, {Encode(toName)}!</p>
              <p style="color:#374151;margin:0 0 1.5rem">
                Recebemos uma solicitação para redefinir a senha da sua conta Comprai.
                Clique no botão abaixo para criar uma nova senha.
              </p>
              <a href="{WebUtility.HtmlEncode(resetLink)}"
                 style="display:inline-block;background:#7c3aed;color:#fff;font-weight:700;
                        padding:.75rem 1.75rem;border-radius:8px;text-decoration:none;font-size:15px">
                Redefinir senha
              </a>
              <p style="color:#9ca3af;font-size:.8125rem;margin:1.25rem 0 0">
                Link válido por 1 hora. Se você não solicitou isso, ignore este e-mail.
              </p>
            </div>
            """;
        return SendAsync(toEmail, "Redefinição de senha — Comprai", html, ct);
    }

    public Task SendAdminInviteEmailAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Inter,sans-serif;max-width:480px;margin:auto;padding:2rem">
              <h2 style="color:#1a1a2e;margin:0 0 1rem;text-align:center">Bem-vindo ao Comprai Admin</h2>
              <p style="color:#374151;margin:0 0 .5rem">Olá, {Encode(toName)}!</p>
              <p style="color:#374151;margin:0 0 1.5rem">
                Sua conta de administrador foi criada. Clique no botão abaixo para confirmar
                seu e-mail e ativar o acesso ao painel administrativo.
              </p>
              <div style="text-align:center;margin:2rem 0">
                <a href="{WebUtility.HtmlEncode(confirmLink)}"
                   style="display:inline-block;background:#7c3aed;color:#fff;font-weight:700;
                          padding:.875rem 2rem;border-radius:10px;text-decoration:none;font-size:15px">
                  ✓ Confirmar cadastro
                </a>
              </div>
              <p style="color:#9ca3af;font-size:.8125rem;margin:1.25rem 0 0;text-align:center">
                Link válido por 24 horas.
              </p>
            </div>
            """;
        return SendAsync(toEmail, "Confirme seu cadastro — Comprai Admin", html, ct);
    }

    public Task SendTransactionalAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
        => SendAsync(toEmail, subject, htmlBody, ct);

    private static string Encode(string s) => WebUtility.HtmlEncode(s);
}

/// <summary>
/// Implementação nula: loga o código sem enviar e-mail.
/// Usada quando nenhum provider está configurado.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NullEmailService(ILogger<NullEmailService> logger) : IEmailService
{
    public Task SendVerificationEmailAsync(string toEmail, string toName, string code, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[NullEmailService] Código de verificação {Code} para {Email} (não enviado — sem RESEND_API_KEY)",
            code, toEmail);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[NullEmailService] Link de reset de senha para {Email}: {Link} (não enviado — sem RESEND_API_KEY)",
            toEmail, resetLink);
        return Task.CompletedTask;
    }

    public Task SendAdminInviteEmailAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[NullEmailService] Link de confirmação admin para {Email}: {Link} (não enviado — sem RESEND_API_KEY)",
            toEmail, confirmLink);
        return Task.CompletedTask;
    }

    public Task SendTransactionalAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[NullEmailService] E-mail transacional para {Email} | {Subject} (não enviado — sem provider configurado)",
            toEmail, subject);
        return Task.CompletedTask;
    }
}
