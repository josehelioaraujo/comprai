namespace UcpAgent.SharedKernel.Ports;

public interface IPaymentPort
{
    Task<PaymentResultDto> ProcessAsync(
        string orderId,
        decimal amount,
        string currency,
        PaymentMethodDto method,
        CancellationToken cancellationToken = default);
}

public record PaymentMethodDto(
    string Provider,       // "mock" | "stripe" | "efi"
    string? CardToken,     // stripe payment method id
    string? PixKey         // efi pix key
);

public record PaymentResultDto(
    string PaymentId,
    bool   Success,
    string Status,         // "approved" | "pending" | "failed"
    string? PixQrCode,     // base64 qr code (efi)
    string? PixCopiaECola, // pix copia e cola (efi)
    string? Error
);
