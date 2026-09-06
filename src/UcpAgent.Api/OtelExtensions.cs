using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace UcpAgent.Api;

public static class OtelExtensions
{
    public static IServiceCollection AddObservabilidade(
        this IServiceCollection services,
        IConfiguration config)
    {
        var endpoint = config["Otel:Endpoint"] ?? "http://localhost:4317";
        var servico  = config["Otel:ServiceName"] ?? "comprai-api";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(servico))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)));

        return services;
    }
}
