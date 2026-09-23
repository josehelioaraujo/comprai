using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class ContainerEndpoints
{
    public static void MapContainerEndpoints(this WebApplication app)
    {
        // GET /api/containers — lista containers via docker ps
        app.MapGet("/api/containers", async (CancellationToken ct) =>
        {
            try
            {
                var result = await RunDockerAsync(
                    "ps -a --format \"{{.Names}}|{{.Status}}|{{.Image}}|{{.Ports}}|{{.ID}}\"", ct);

                if (result.ExitCode != 0)
                    return Results.Problem("docker ps falhou: " + result.Error, statusCode: 503);

                var containers = result.Output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line =>
                    {
                        var parts = line.Split('|');
                        if (parts.Length < 5) return null;
                        var status = parts[1].Trim();
                        return new
                        {
                            name    = parts[0].Trim(),
                            status  = status,
                            healthy = status.StartsWith("Up", StringComparison.OrdinalIgnoreCase),
                            image   = parts[2].Trim(),
                            ports   = parts[3].Trim(),
                            id      = parts[4].Trim()[..Math.Min(12, parts[4].Trim().Length)]
                        };
                    })
                    .Where(c => c != null)
                    .ToList();

                return Results.Ok(new { containers, updatedAt = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 503);
            }
        })
        .WithTags("Containers").WithName("ListContainers").AllowAnonymous();

        // GET /api/containers/{name}/logs — últimas N linhas de log
        app.MapGet("/api/containers/{name}/logs", async (
            string name,
            int? lines,
            CancellationToken ct) =>
        {
            // Validar nome — só letras, números, hífens e underscores
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9_\-]+$"))
                return Results.BadRequest("Nome inválido");

            try
            {
                var n = Math.Min(lines ?? 50, 200);
                var result = await RunDockerAsync($"logs --tail {n} {name}", ct);

                var combined = string.IsNullOrEmpty(result.Output) ? result.Error : result.Output;
                // Remover códigos ANSI de cor/escape
                var ansiRegex = new System.Text.RegularExpressions.Regex(@"\x1B\[[0-9;]*[mKHFJsu]");
                var logLines = combined
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => ansiRegex.Replace(l, ""))
                    .TakeLast(n)
                    .ToList();

                return Results.Ok(new { name, lines = logLines });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 503);
            }
        })
        .WithTags("Containers").WithName("ContainerLogs").AllowAnonymous();

        // POST /api/containers/{name}/restart — reinicia container
        app.MapPost("/api/containers/{name}/restart", async (
            string name,
            HttpContext ctx,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9_\-]+$"))
                return Results.BadRequest("Nome inválido");

            // Requer senha admin
            var pwd = ctx.Request.Headers["X-Admin-Password"].FirstOrDefault();
            var expected = config["Admin:RestartPassword"];
            if (string.IsNullOrEmpty(expected) || pwd != expected)
                return Results.Unauthorized();

            try
            {
                var result = await RunDockerAsync($"restart {name}", ct);
                return result.ExitCode == 0
                    ? Results.Ok(new { message = $"{name} reiniciado" })
                    : Results.Problem(result.Error, statusCode: 503);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 503);
            }
        })
        .WithTags("Containers").WithName("RestartContainer").AllowAnonymous();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunDockerAsync(
        string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("docker", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var proc = new Process { StartInfo = psi };
        proc.Start();

        var output = await proc.StandardOutput.ReadToEndAsync(ct);
        var error  = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        return (proc.ExitCode, output.Trim(), error.Trim());
    }
}
