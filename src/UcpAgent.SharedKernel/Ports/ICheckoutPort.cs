namespace UcpAgent.SharedKernel.Ports;

public interface ICheckoutPort
{
    Task<CheckoutResultDto> ProcessAsync(string sessionId, CustomerDto customer, CancellationToken cancellationToken = default);
}

public record CustomerDto(string Name, string Email, string Phone, string Address);
public record CheckoutResultDto(string OrderId, string Status, string? CheckoutUrl);
