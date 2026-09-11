using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Adapters;

public static class EfiPaymentExtensions
{
    public static IServiceCollection AddEfiPayment(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration.GetSection("EfiPay").Get<EfiPayOptions>() ?? new EfiPayOptions();
        services.AddSingleton(opts);

        services.AddHttpClient("efipay-pix", c =>
        {
            c.BaseAddress = new Uri(opts.BaseUrl);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();

            if (!string.IsNullOrWhiteSpace(opts.CertificateBase64))
            {
                var certBytes = Convert.FromBase64String(opts.CertificateBase64);
                var cert = new X509Certificate2(
                    certBytes,
                    opts.CertificatePassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                handler.ClientCertificates.Add(cert);
            }
            else if (!string.IsNullOrWhiteSpace(opts.CertificatePath))
            {
                var cert = new X509Certificate2(
                    opts.CertificatePath,
                    opts.CertificatePassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                handler.ClientCertificates.Add(cert);
            }

            return handler;
        });

        services.AddSingleton<IPaymentPort>(sp =>
            new EfiPaymentAdapter(
                sp.GetRequiredService<EfiPayOptions>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<EfiPaymentAdapter>>()));

        return services;
    }
}
