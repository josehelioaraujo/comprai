using Stripe;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Payment;

/// <summary>
/// Adapter Stripe — usa PaymentIntents API.
/// Requer Stripe:SecretKey em appsettings / env vars.
/// </summary>
public sealed class StripePaymentAdapter : IPaymentPort
{
    public StripePaymentAdapter(IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey não configurado");
    }

    public async Task<PaymentResultDto> ProcessAsync(
        string orderId,
        decimal amount,
        string currency,
        PaymentMethodDto method,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount             = (long)(amount * 100), // centavos
                Currency           = currency.ToLower(),   // "brl"
                PaymentMethod      = method.CardToken,
                Confirm            = true,
                ReturnUrl          = "https://comprai.app/payment/return",
                Metadata           = new Dictionary<string, string>
                {
                    ["order_id"] = orderId
                }
            };

            var service = new PaymentIntentService();
            var intent  = await service.CreateAsync(options, cancellationToken: cancellationToken);

            var success = intent.Status is "succeeded" or "requires_capture";

            return new PaymentResultDto(
                PaymentId:     intent.Id,
                Success:       success,
                Status:        intent.Status,
                PixQrCode:     null,
                PixCopiaECola: null,
                Error:         success ? null : $"Stripe status: {intent.Status}");
        }
        catch (StripeException ex)
        {
            return new PaymentResultDto(
                PaymentId:     string.Empty,
                Success:       false,
                Status:        "failed",
                PixQrCode:     null,
                PixCopiaECola: null,
                Error:         ex.StripeError?.Message ?? ex.Message);
        }
    }
}
