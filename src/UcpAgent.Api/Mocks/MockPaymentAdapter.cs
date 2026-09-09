using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

/// <summary>
/// Adapter de pagamento para dev/demo — aprova tudo sem chamada externa.
/// </summary>
public sealed class MockPaymentAdapter : IPaymentPort
{
    public Task<PaymentResultDto> ProcessAsync(
        string orderId,
        decimal amount,
        string currency,
        PaymentMethodDto method,
        CancellationToken cancellationToken = default)
    {
        var paymentId = $"MOCK-PAY-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var result = new PaymentResultDto(
            PaymentId:     paymentId,
            Success:       true,
            Status:        "approved",
            PixQrCode:     null,
            PixCopiaECola: null,
            Error:         null);

        return Task.FromResult(result);
    }
}
