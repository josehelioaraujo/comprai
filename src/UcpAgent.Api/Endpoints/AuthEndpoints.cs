using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using UcpAgent.Api.Auth;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest req,
            IAuthPort auth,
            JwtService jwt) =>
        {
            var customer = await auth.RegisterAsync(req);
            if (customer is null)
                return Results.Conflict(new { error = "Email já cadastrado." });

            return Results.Ok(new AuthResponse(jwt.Generate(customer), customer));
        });

        group.MapPost("/login", async (
            [FromBody] LoginRequest req,
            IAuthPort auth,
            JwtService jwt) =>
        {
            var customer = await auth.LoginAsync(req.Email, req.Password);
            if (customer is null) return Results.Unauthorized();

            return Results.Ok(new AuthResponse(jwt.Generate(customer), customer));
        });

        group.MapPost("/callback", async (
            [FromBody] SsoCallbackRequest req,
            IAuthPort auth,
            JwtService jwt) =>
        {
            var customer = await auth.SsoCallbackAsync(req);
            return Results.Ok(new AuthResponse(jwt.Generate(customer), customer));
        });

        group.MapGet("/me", async (HttpContext ctx, IAuthPort auth) =>
        {
            var customerId = JwtService.GetCustomerId(ctx.User);
            if (customerId is null) return Results.Unauthorized();

            var customer = await auth.GetByIdAsync(customerId);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }).RequireAuthorization();
    }

    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, AuthCustomerDto Customer);
}
