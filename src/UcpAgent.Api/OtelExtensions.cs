using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace UcpAgent.Api;

[ExcludeFromCodeCoverage]
public static class OtelExtensions
{
    public static IServiceCollection AddObservabilidade(
        this IServiceCollection services,
        IConfiguration config)
    {
        var endpointBase = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                        ?? config["Otel:Endpoint"]
                        ?? "http://localhost:4318";

        var baseUrl = endpointBase.TrimEnd('/').Replace(":4317", ":4318");

        var traceEndpoint = baseUrl + "/v1/traces";
        var logEndpoint   = baseUrl + "/v1/logs";

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
                    o.Endpoint = new Uri(traceEndpoint);
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                }))
            .WithLogging(logging => logging
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(logEndpoint);
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                }));

        return services;
    }
}
