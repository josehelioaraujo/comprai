using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Ports;

[ExcludeFromCodeCoverage]
public interface IPaymentPort
{
    Task<PaymentResultDto> ProcessAsync(
        string orderId,
        decimal amount,
        string currency,
        PaymentMethodDto method,
        CancellationToken cancellationToken = default);
}

[ExcludeFromCodeCoverage]
public record PaymentMethodDto(
    string Provider,
    string? CardToken,
    string? PixKey
);

[ExcludeFromCodeCoverage]
public record PaymentResultDto(
    string PaymentId,
    bool   Success,
    string Status,
    string? PixQrCode,
    string? PixCopiaECola,
    string? Error
);
