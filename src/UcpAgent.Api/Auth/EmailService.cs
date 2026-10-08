using System.Diagnostics.CodeAnalysis;
using Resend;

namespace UcpAgent.Api.Auth;

/// <summary>Abstração de envio de e-mail transacional.</summary>
public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string toName, string code, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default);
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

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);
}

/// <summary>
/// Implementação nula: loga o código sem enviar e-mail.
/// Usada quando RESEND_API_KEY não está configurado.
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
}
