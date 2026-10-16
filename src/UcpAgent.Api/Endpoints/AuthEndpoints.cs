using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using UcpAgent.Api.Auth;
using UcpAgent.Api.RateLimit;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class AuthEndpoints
{
    private static readonly TimeSpan RefreshTokenTtl      = TimeSpan.FromDays(7);
    private static readonly TimeSpan VerificationCodeTtl  = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordResetTokenTtl = TimeSpan.FromHours(1);

    // Caracteres sem ambiguidade visual (sem 0/1/I/O)
    private const string OtpChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        MapAdminAuthEndpoints(app, PasswordResetTokenTtl);
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest req,
            IAuthPort auth, ISessionPort? sessions, JwtService jwt, HttpContext ctx) =>
        {
            var customer = await auth.RegisterAsync(req);
            if (customer is null) return Results.Conflict(new { error = "Email já cadastrado." });
            await LinkSessionAsync(sessions, ctx, customer.Id);
            return Results.Ok(await IssueTokenPairAsync(auth, jwt, customer));
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        group.MapPost("/login", async (
            [FromBody] LoginRequest req,
            IAuthPort auth, ISessionPort? sessions, JwtService jwt, TotpService totp, HttpContext ctx) =>
        {
            var customer = await auth.LoginAsync(req.Email, req.Password);
            if (customer is null) return Results.Unauthorized();

            // V060-F2: se TOTP ativado, emite tempToken e exige segundo fator
            if (customer.TotpEnabled)
            {
                var tempToken = totp.GenerateTempToken(customer.Id);
                return Results.Ok(new { requires2fa = true, tempToken });
            }

            await LinkSessionAsync(sessions, ctx, customer.Id);
            return Results.Ok(await IssueTokenPairAsync(auth, jwt, customer));
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        group.MapPost("/callback", async (
            [FromBody] SsoCallbackRequest req,
            IAuthPort auth, ISessionPort? sessions, JwtService jwt, HttpContext ctx) =>
        {
            var customer = await auth.SsoCallbackAsync(req);
            await LinkSessionAsync(sessions, ctx, customer.Id);
            return Results.Ok(await IssueTokenPairAsync(auth, jwt, customer));
        });

        // V059-F2: rotação de refresh token
        // POST /api/auth/refresh  body: { "refreshToken": "<plain>" }
        group.MapPost("/refresh", async (
            [FromBody] RefreshRequest req,
            IAuthPort auth, JwtService jwt) =>
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Results.BadRequest(new { error = "refreshToken obrigatório." });

            string hash;
            try { hash = JwtService.HashRefreshToken(req.RefreshToken); }
            catch { return Results.Unauthorized(); }

            var customer = await auth.GetByRefreshTokenHashAsync(hash);
            if (customer is null) return Results.Unauthorized();

            // rotação — invalida o token atual antes de emitir o novo par
            await auth.RevokeRefreshTokenAsync(customer.Id);
            return Results.Ok(await IssueTokenPairAsync(auth, jwt, customer));
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // POST /api/auth/logout
        group.MapPost("/logout", async (HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is not null)
                await auth.RevokeRefreshTokenAsync(customerId);
            return Results.NoContent();
        }).RequireAuthorization();

        // ── V061-F2: Logout de todos os dispositivos ──────────────────────────
        // POST /api/auth/logout-all
        group.MapPost("/logout-all", async (HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();
            await auth.RevokeAllRefreshTokensAsync(customerId);
            return Results.Ok(new { message = "Sessão encerrada em todos os dispositivos." });
        }).RequireAuthorization();

        // ── V061-F1: Recuperação de senha ─────────────────────────────────────
        // POST /api/auth/forgot-password  body: { "email": "..." }
        // Resposta sempre 204 — não revela se o e-mail existe (proteção de enumeração).
        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest req,
            IAuthPort auth, IEmailService email, IConfiguration config, ILoggerFactory lf) =>
        {
            var log = lf.CreateLogger("Auth.ForgotPassword");
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.NoContent();

            var (plainToken, tokenHash) = JwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.Add(PasswordResetTokenTtl);
            var emailNorm = req.Email.Trim().ToLowerInvariant();

            var saved = await auth.SavePasswordResetTokenByEmailAsync(emailNorm, tokenHash, expiresAt);
            if (saved)
            {
                var found = await auth.GetByPasswordResetTokenAsync(tokenHash);
                if (found is not null)
                {
                    var webUrl = config["App:WebUrl"] ?? "http://2.25.122.11:3002";
                    var resetLink = $"{webUrl}/auth/reset-password?token={Uri.EscapeDataString(plainToken)}";
                    try { await email.SendPasswordResetEmailAsync(found.Email, found.Name, resetLink); }
                    catch (Exception ex) { log.LogError(ex, "[ForgotPassword] Falha ao enviar e-mail para {Email}", emailNorm); }
                }
            }

            return Results.NoContent();
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // POST /api/auth/reset-password  body: { "token": "...", "newPassword": "..." }
        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest req,
            IAuthPort auth) =>
        {
            if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Results.BadRequest(new { error = "token e newPassword são obrigatórios." });

            if (req.NewPassword.Length < 8)
                return Results.BadRequest(new { error = "A senha deve ter pelo menos 8 caracteres." });

            string tokenHash;
            try { tokenHash = JwtService.HashRefreshToken(req.Token); }
            catch { return Results.BadRequest(new { error = "Token inválido." }); }

            var ok = await auth.ResetPasswordAsync(tokenHash, req.NewPassword);
            return ok
                ? Results.Ok(new { message = "Senha redefinida com sucesso." })
                : Results.BadRequest(new { error = "Token inválido ou expirado." });
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // GET /api/auth/me
        group.MapGet("/me", async (HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();
            var customer = await auth.GetByIdAsync(customerId);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }).RequireAuthorization();

        // PUT /api/auth/me
        group.MapPut("/me", async (
            [FromBody] UpdateProfileBody body,
            HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();
            var updated = await auth.UpdateProfileAsync(
                new UpdateProfileRequest(customerId, body.Name, body.Phone, body.Document));
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization();

        // POST /api/auth/me/address
        group.MapPost("/me/address", async (
            [FromBody] SaveAddressRequest req,
            HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();
            var address = await auth.SaveAddressAsync(req with { CustomerId = customerId });
            return address is null ? Results.Problem("Erro ao salvar endereço.") : Results.Ok(address);
        }).RequireAuthorization();

        // ── V060-F1: Email Verification ──────────────────────────────────────

        // POST /api/auth/send-verification
        // Gera OTP de 6 chars, persiste e envia via Resend (ou loga se NullEmailService)
        group.MapPost("/send-verification", async (
            HttpContext ctx, IAuthPort auth, IEmailService email) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();

            var customer = await auth.GetByIdAsync(customerId);
            if (customer is null) return Results.NotFound();

            if (customer.EmailVerified)
                return Results.Ok(new { message = "Email já verificado." });

            var code = GenerateOtp();
            await auth.SaveVerificationCodeAsync(customerId, code, DateTime.UtcNow.Add(VerificationCodeTtl));
            await email.SendVerificationEmailAsync(customer.Email, customer.Name, code);

            return Results.Ok(new { message = "Código de verificação enviado." });
        }).RequireAuthorization().RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // POST /api/auth/verify-email  body: { "code": "JQCYMX" }
        group.MapPost("/verify-email", async (
            [FromBody] VerifyEmailRequest req,
            HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Code))
                return Results.BadRequest(new { error = "Código obrigatório." });

            var ok = await auth.VerifyEmailCodeAsync(customerId, req.Code.ToUpperInvariant().Trim());
            return ok
                ? Results.Ok(new { message = "Email verificado com sucesso." })
                : Results.BadRequest(new { error = "Código inválido ou expirado." });
        }).RequireAuthorization().RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // ── V060-F2: TOTP 2FA ─────────────────────────────────────────────────

        // POST /api/auth/2fa/setup — gera secret e URI otpauth para QR Code
        // Requer JWT normal (usuário autenticado)
        group.MapPost("/2fa/setup", async (
            HttpContext ctx, IAuthPort auth, TotpService totp) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();

            var customer = await auth.GetByIdAsync(customerId);
            if (customer is null) return Results.NotFound();

            var secret = totp.GenerateSecret();
            await auth.SaveTotpSecretAsync(customerId, secret);
            var uri = totp.BuildOtpAuthUri(secret, customer.Email);

            return Results.Ok(new { secret, otpAuthUri = uri });
        }).RequireAuthorization();

        // POST /api/auth/2fa/enable — confirma primeiro código e ativa TOTP
        // body: { "code": "123456" }
        group.MapPost("/2fa/enable", async (
            [FromBody] TotpCodeRequest req,
            HttpContext ctx, IAuthPort auth, TotpService totp) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Code))
                return Results.BadRequest(new { error = "Código obrigatório." });

            var (enabled, secret) = await auth.GetTotpDataAsync(customerId);
            if (enabled)        return Results.BadRequest(new { error = "TOTP já ativado." });
            if (secret is null) return Results.BadRequest(new { error = "Execute /2fa/setup primeiro." });

            if (!totp.Verify(secret, req.Code.Trim()))
                return Results.BadRequest(new { error = "Código inválido." });

            var ok = await auth.EnableTotpAsync(customerId);
            return ok
                ? Results.Ok(new { message = "2FA ativado com sucesso." })
                : Results.Problem("Falha ao ativar 2FA.");
        }).RequireAuthorization().RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // POST /api/auth/2fa/verify — valida código durante o login (usa tempToken)
        // body: { "tempToken": "...", "code": "123456" }
        group.MapPost("/2fa/verify", async (
            [FromBody] TotpVerifyRequest req,
            IAuthPort auth, ISessionPort? sessions, JwtService jwt, TotpService totp, HttpContext ctx) =>
        {
            if (string.IsNullOrWhiteSpace(req.TempToken) || string.IsNullOrWhiteSpace(req.Code))
                return Results.BadRequest(new { error = "tempToken e code são obrigatórios." });

            var customerId = totp.ValidateTempToken(req.TempToken);
            if (customerId is null) return Results.Unauthorized();

            var (enabled, secret) = await auth.GetTotpDataAsync(customerId);
            if (!enabled || secret is null) return Results.Unauthorized();

            if (!totp.Verify(secret, req.Code.Trim()))
                return Results.BadRequest(new { error = "Código inválido ou expirado." });

            var customer = await auth.GetByIdAsync(customerId);
            if (customer is null) return Results.Unauthorized();

            await LinkSessionAsync(sessions, ctx, customer.Id);
            return Results.Ok(await IssueTokenPairAsync(auth, jwt, customer));
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // POST /api/auth/2fa/disable — desativa TOTP (requer código válido)
        // body: { "code": "123456" }
        group.MapPost("/2fa/disable", async (
            [FromBody] TotpCodeRequest req,
            HttpContext ctx, IAuthPort auth, TotpService totp) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(req.Code))
                return Results.BadRequest(new { error = "Código obrigatório." });

            var (enabled, secret) = await auth.GetTotpDataAsync(customerId);
            if (!enabled || secret is null) return Results.BadRequest(new { error = "TOTP não está ativo." });

            if (!totp.Verify(secret, req.Code.Trim()))
                return Results.BadRequest(new { error = "Código inválido." });

            await auth.DisableTotpAsync(customerId);
            return Results.Ok(new { message = "2FA desativado com sucesso." });
        }).RequireAuthorization().RequireRateLimiting(RateLimitExtensions.AuthPolicy);
    }

    // ── V070: Admin Registration ──────────────────────────────────────────────

    // POST /api/auth/admin/register
    // Cria usuário com role='admin' (email_verified=FALSE) e envia e-mail de confirmação.
    // Resposta: 204 sempre (não revela se e-mail já existe — proteção de enumeração).
    private static void MapAdminAuthEndpoints(WebApplication app, TimeSpan PasswordResetTokenTtl)
    {
        var adminGroup = app.MapGroup("/api/auth/admin").WithTags("Auth");

        adminGroup.MapPost("/register", async (
            [FromBody] AdminRegisterRequest req,
            IAuthPort auth, IEmailService email, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "email e password são obrigatórios." });

            if (req.Password.Length < 8)
                return Results.BadRequest(new { error = "A senha deve ter pelo menos 8 caracteres." });

            var emailNorm = req.Email.Trim().ToLowerInvariant();
            var customer = await auth.RegisterAdminAsync(emailNorm, req.Password);
            if (customer is null)
                return Results.Conflict(new { error = "E-mail já cadastrado." });

            // Gera token de confirmação e salva no campo password_reset_token_hash (TTL 24h)
            var (plainToken, tokenHash) = JwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddHours(24);
            await auth.SavePasswordResetTokenByEmailAsync(emailNorm, tokenHash, expiresAt);

            var adminUrl = config["App:AdminUrl"] ?? "http://2.25.122.11:3004";
            var confirmLink = $"{adminUrl}/api/auth/admin/confirm-email?token={Uri.EscapeDataString(plainToken)}";
            try { await email.SendAdminInviteEmailAsync(customer.Email, customer.Name, confirmLink); }
            catch { /* best-effort */ }

            return Results.NoContent();
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        // GET /api/auth/admin/confirm-email?token=...
        // Confirma e-mail do admin via link clicável. Retorna HTML.
        adminGroup.MapGet("/confirm-email", async (
            [FromQuery] string? token,
            IAuthPort auth) =>
        {
            if (string.IsNullOrWhiteSpace(token))
                return Results.Content(HtmlConfirm(false, "Token ausente."), "text/html");

            string tokenHash;
            try { tokenHash = JwtService.HashRefreshToken(token); }
            catch { return Results.Content(HtmlConfirm(false, "Token inválido."), "text/html"); }

            var ok = await auth.ConfirmAdminEmailAsync(tokenHash);
            return Results.Content(
                ok ? HtmlConfirm(true, null) : HtmlConfirm(false, "Token inválido ou expirado."),
                "text/html");
        });

        // POST /api/auth/admin/forgot-password
        // Envia e-mail de reset com link apontando para App:AdminUrl (não App:WebUrl).
        adminGroup.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest req,
            IAuthPort auth, IEmailService email, IConfiguration config, ILoggerFactory lf) =>
        {
            var log = lf.CreateLogger("Auth.AdminForgotPassword");
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.NoContent();

            var (plainToken, tokenHash) = JwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.Add(PasswordResetTokenTtl);
            var emailNorm = req.Email.Trim().ToLowerInvariant();

            var saved = await auth.SavePasswordResetTokenByEmailAsync(emailNorm, tokenHash, expiresAt);
            if (saved)
            {
                var found = await auth.GetByPasswordResetTokenAsync(tokenHash);
                if (found is not null)
                {
                    var adminUrl = config["App:AdminUrl"] ?? "http://2.25.122.11:3004";
                    var resetLink = $"{adminUrl}/reset-password?token={Uri.EscapeDataString(plainToken)}";
                    try { await email.SendPasswordResetEmailAsync(found.Email, found.Name, resetLink); }
                    catch (Exception ex) { log.LogError(ex, "[AdminForgotPassword] Falha ao enviar e-mail para {Email}", emailNorm); }
                }
            }

            return Results.NoContent();
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);
    }

    private static string HtmlConfirm(bool success, string? message) => $$"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="utf-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>Comprai Admin — {{(success ? "Cadastro confirmado" : "Erro")}}</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{font-family:Inter,system-ui,sans-serif;background:#030712;
                 min-height:100vh;display:flex;align-items:center;justify-content:center}
            .card{background:#111827;border:1px solid #1f2937;border-radius:20px;
                  padding:3rem 2.5rem;max-width:420px;width:90%;text-align:center}
            .icon{width:64px;height:64px;border-radius:16px;
                  background:{{(success ? "#14532d" : "#450a0a")}};
                  display:inline-flex;align-items:center;justify-content:center;margin-bottom:1.5rem}
            h1{font-size:1.375rem;font-weight:700;color:#f9fafb;margin-bottom:.75rem}
            p{color:#9ca3af;font-size:.9375rem;line-height:1.6;margin-bottom:1.75rem}
            a{display:inline-block;background:#7c3aed;color:#fff;font-weight:700;
              padding:.75rem 1.75rem;border-radius:10px;text-decoration:none;font-size:15px}
            a:hover{background:#6d28d9}
          </style>
        </head>
        <body>
          <div class="card">
            <div class="icon">
              {{(success
                ? """<svg width="32" height="32" fill="none" stroke="#4ade80" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>"""
                : """<svg width="32" height="32" fill="none" stroke="#f87171" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/></svg>""")}}
            </div>
            <h1>{{(success ? "Cadastro confirmado!" : "Link inválido")}}</h1>
            <p>{{(success
              ? "Seu e-mail foi verificado com sucesso. Você já pode acessar o painel administrativo do Comprai."
              : (message ?? "O link de confirmação é inválido ou expirou. Solicite um novo cadastro."))}}</p>
            <a href="/login">Ir para o login</a>
          </div>
        </body>
        </html>
        """;

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<AuthResponse> IssueTokenPairAsync(IAuthPort auth, JwtService jwt, AuthCustomerDto customer)
    {
        var accessToken = jwt.Generate(customer);
        var (plain, hash) = JwtService.GenerateRefreshToken();
        await auth.SaveRefreshTokenAsync(customer.Id, hash, DateTime.UtcNow.Add(RefreshTokenTtl));
        return new AuthResponse(accessToken, plain, customer);
    }

    private static async Task LinkSessionAsync(ISessionPort? sessions, HttpContext ctx, string customerId)
    {
        if (sessions is null) return;
        if (!Guid.TryParse(customerId, out var customerGuid)) return;
        var header = ctx.Request.Headers["X-Session-Id"].FirstOrDefault();
        if (!Guid.TryParse(header, out var sessionGuid)) return;
        try { await sessions.LinkCustomerAsync(sessionGuid, customerGuid); }
        catch { /* best-effort */ }
    }

    /// <summary>Gera OTP de 6 caracteres usando RandomNumberGenerator (thread-safe, CSPRNG).</summary>
    private static string GenerateOtp()
    {
        return string.Create(6, OtpChars, static (span, chars) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        });
    }

    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record UpdateProfileBody(string Name, string? Phone = null, string? Document = null);
    public record VerifyEmailRequest(string Code);
    public record AuthResponse(string Token, string RefreshToken, AuthCustomerDto Customer);
    // V060-F2
    public record TotpCodeRequest(string Code);
    public record TotpVerifyRequest(string TempToken, string Code);
    // V061-F1
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Token, string NewPassword);
    // V070
    public record AdminRegisterRequest(string Email, string Password);
}
