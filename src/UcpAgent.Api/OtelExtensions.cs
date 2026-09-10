using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace UcpAgent.Api;

public static class OtelExtensions
{
    public static IServiceCollection AddObservabilidade(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Prioridade: env var padrão OTel → appsettings Otel:Endpoint → localhost
        var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                    ?? config["Otel:Endpoint"]
                    ?? "http://localhost:4317";

        var servico = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                   ?? config["Otel:ServiceName"]
                   ?? "comprai-api";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(servico))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(endpoint);
                    o.Protocol = OtlpExportProtocol.Grpc;
                }));

        return services;
    }
}
