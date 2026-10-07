using System.Diagnostics.CodeAnalysis;
using Resend;

namespace UcpAgent.Api.Auth;

/// <summary>Abstração de envio de e-mail transacional.</summary>
public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string toName, string code, CancellationToken ct = default);
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
}
