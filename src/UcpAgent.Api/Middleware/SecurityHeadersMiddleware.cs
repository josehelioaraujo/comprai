using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.Api.Middleware;

[ExcludeFromCodeCoverage]
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Impede clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Impede MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Controla informações de referência
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // HSTS (1 ano) — ativo apenas em HTTPS
        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Restringe funcionalidades do browser
        headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=self, usb=()";

        // Content Security Policy
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' fonts.googleapis.com; " +
            "font-src 'self' fonts.gstatic.com; " +
            "img-src 'self' data: blob:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        // Remove header que expõe tecnologia usada
        headers.Remove("X-Powered-By");
        headers.Remove("Server");

        await next(context);
    }
}
