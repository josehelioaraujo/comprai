using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace UcpAgent.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class ContainerEndpoints
{
    private static readonly string[] CriticalContainers =
        ["comprai-redis", "comprai-kafka", "comprai-rabbitmq", "comprai-loki", "comprai-prometheus"];

    private static readonly Regex NameRegex  = new(@"^[a-zA-Z0-9_\-]+$", RegexOptions.Compiled);
    private static readonly Regex AnsiRegex  = new(@"\x1B\[[0-9;]*[mKHFJsu]", RegexOptions.Compiled);

    public static void MapContainerEndpoints(this WebApplication app)
    {
        // GET /api/containers
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
                        var status   = parts[1].Trim();
                        var name     = parts[0].Trim();
                        var running  = status.StartsWith("Up", StringComparison.OrdinalIgnoreCase);
                        return new
                        {
                            name,
                            status,
                            healthy  = running,
                            image    = parts[2].Trim(),
                            ports    = parts[3].Trim(),
                            id       = parts[4].Trim()[..Math.Min(12, parts[4].Trim().Length)],
                            critical = CriticalContainers.Contains(name)
                        };
                    })
                    .Where(c => c != null)
                    .ToList();

                return Results.Ok(new { containers, updatedAt = DateTime.UtcNow });
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Containers").WithName("ListContainers").AllowAnonymous();

        // GET /api/containers/{name}/logs
        app.MapGet("/api/containers/{name}/logs", async (string name, int? lines, CancellationToken ct) =>
        {
            if (!NameRegex.IsMatch(name)) return Results.BadRequest("Nome inválido");
            try
            {
                var n      = Math.Min(lines ?? 50, 200);
                var result = await RunDockerAsync($"logs --tail {n} {name}", ct);
                var combined = string.IsNullOrEmpty(result.Output) ? result.Error : result.Output;
                var logLines = combined
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => AnsiRegex.Replace(l, ""))
                    .TakeLast(n)
                    .ToList();
                return Results.Ok(new { name, lines = logLines });
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Containers").WithName("ContainerLogs").AllowAnonymous();

        // POST /api/containers/{name}/restart — requer senha admin
        app.MapPost("/api/containers/{name}/restart", async (
            string name, HttpContext ctx, IConfiguration config, CancellationToken ct) =>
        {
            if (!NameRegex.IsMatch(name)) return Results.BadRequest("Nome inválido");
            if (!CheckAdmin(ctx, config))  return Results.Unauthorized();
            try
            {
                var result = await RunDockerAsync($"restart {name}", ct);
                return result.ExitCode == 0
                    ? Results.Ok(new { message = $"{name} reiniciado com sucesso" })
                    : Results.Problem(result.Error, statusCode: 503);
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Containers").WithName("RestartContainer").AllowAnonymous();

        // POST /api/containers/{name}/stop — requer senha; bloqueado para críticos
        app.MapPost("/api/containers/{name}/stop", async (
            string name, HttpContext ctx, IConfiguration config, CancellationToken ct) =>
        {
            if (!NameRegex.IsMatch(name)) return Results.BadRequest("Nome inválido");
            if (!CheckAdmin(ctx, config))  return Results.Unauthorized();
            if (CriticalContainers.Contains(name))
                return Results.Problem($"Container crítico '{name}' não pode ser parado por aqui.", statusCode: 403);
            try
            {
                var result = await RunDockerAsync($"stop {name}", ct);
                return result.ExitCode == 0
                    ? Results.Ok(new { message = $"{name} parado" })
                    : Results.Problem(result.Error, statusCode: 503);
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Containers").WithName("StopContainer").AllowAnonymous();

        // GET /api/containers/{name}/stats — CPU e memoria via docker stats
        app.MapGet("/api/containers/{name}/stats", async (string name, CancellationToken ct) =>
        {
            if (!NameRegex.IsMatch(name)) return Results.BadRequest("Nome invalido");
            try
            {
                var fmt = "\"{{.CPUPerc}}|{{.MemUsage}}|{{.MemPerc}}|{{.NetIO}}|{{.BlockIO}}\"";
                var result = await RunDockerAsync($"stats --no-stream --format {fmt} {name}", ct);

                if (result.ExitCode != 0 || string.IsNullOrEmpty(result.Output))
                    return Results.Ok(new { name, available = false });

                var parts = result.Output.Split('|');
                return Results.Ok(new
                {
                    name,
                    available  = true,
                    cpuPercent = parts.Length > 0 ? parts[0].Trim() : "-",
                    memUsage   = parts.Length > 1 ? parts[1].Trim() : "-",
                    memPercent = parts.Length > 2 ? parts[2].Trim() : "-",
                    netIO      = parts.Length > 3 ? parts[3].Trim() : "-",
                    blockIO    = parts.Length > 4 ? parts[4].Trim() : "-",
                    updatedAt  = DateTime.UtcNow
                });
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Containers").WithName("ContainerStats").AllowAnonymous();

        // POST /api/containers/{name}/start — sem senha (baixo risco)
        app.MapPost("/api/containers/{name}/start", async (string name, CancellationToken ct) =>
        {
            if (!NameRegex.IsMatch(name)) return Results.BadRequest("Nome inválido");
            try
            {
                var result = await RunDockerAsync($"start {name}", ct);
                return result.ExitCode == 0
                    ? Results.Ok(new { message = $"{name} iniciado" })
                    : Results.Problem(result.Error, statusCode: 503);
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Containers").WithName("StartContainer").AllowAnonymous();
    }

    private static bool CheckAdmin(HttpContext ctx, IConfiguration config)
    {
        var pwd      = ctx.Request.Headers["X-Admin-Password"].FirstOrDefault();
        var expected = config["Admin:RestartPassword"];
        return !string.IsNullOrEmpty(expected) && pwd == expected;
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
