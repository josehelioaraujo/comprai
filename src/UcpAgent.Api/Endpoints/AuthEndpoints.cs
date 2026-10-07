using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using UcpAgent.Api.Auth;
using UcpAgent.Api.RateLimit;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // V059-F1B: rate limit — 5 tentativas / 60s por IP (brute force prevention)
        group.MapPost("/register", async (
            [FromBody] RegisterRequest req,
            IAuthPort auth, ISessionPort? sessions, JwtService jwt, HttpContext ctx) =>
        {
            var customer = await auth.RegisterAsync(req);
            if (customer is null) return Results.Conflict(new { error = "Email já cadastrado." });
            await LinkSessionAsync(sessions, ctx, customer.Id);
            return Results.Ok(new AuthResponse(jwt.Generate(customer), customer));
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        group.MapPost("/login", async (
            [FromBody] LoginRequest req,
            IAuthPort auth, ISessionPort? sessions, JwtService jwt, HttpContext ctx) =>
        {
            var customer = await auth.LoginAsync(req.Email, req.Password);
            if (customer is null) return Results.Unauthorized();
            await LinkSessionAsync(sessions, ctx, customer.Id);
            return Results.Ok(new AuthResponse(jwt.Generate(customer), customer));
        }).RequireRateLimiting(RateLimitExtensions.AuthPolicy);

        group.MapPost("/callback", async (
            [FromBody] SsoCallbackRequest req,
            IAuthPort auth, ISessionPort? sessions, JwtService jwt, HttpContext ctx) =>
        {
            var customer = await auth.SsoCallbackAsync(req);
            await LinkSessionAsync(sessions, ctx, customer.Id);
            return Results.Ok(new AuthResponse(jwt.Generate(customer), customer));
        });

        // GET /api/auth/me — retorna customer com endereços
        group.MapGet("/me", async (HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();
            var customer = await auth.GetByIdAsync(customerId);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }).RequireAuthorization();

        // PUT /api/auth/me — atualiza nome, telefone, documento
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

        // POST /api/auth/me/address — salva endereço de entrega
        group.MapPost("/me/address", async (
            [FromBody] SaveAddressRequest req,
            HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();
            var address = await auth.SaveAddressAsync(req with { CustomerId = customerId });
            return address is null ? Results.Problem("Erro ao salvar endereço.") : Results.Ok(address);
        }).RequireAuthorization();
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

    public record LoginRequest(string Email, string Password);
    public record UpdateProfileBody(string Name, string? Phone = null, string? Document = null);
    public record AuthResponse(string Token, AuthCustomerDto Customer);
}
