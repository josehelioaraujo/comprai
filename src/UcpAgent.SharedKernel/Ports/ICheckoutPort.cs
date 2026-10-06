namespace UcpAgent.SharedKernel.Ports;

public interface ICheckoutPort
{
    Task<CheckoutResultDto> ProcessAsync(
        string      sessionId,
        CustomerDto customer,
        Guid?       authenticatedCustomerId = null,
        CancellationToken cancellationToken = default);
}

public record CustomerDto(
    string  Name,
    string  Email,
    string  Phone,
    string  Address,      // campo legado — mantido para compatibilidade
    string? Cep           = null,
    string? Street        = null,
    string? Number        = null,
    string? Complement    = null,
    string? Neighborhood  = null,
    string? City          = null,
    string? State         = null,
    string? Document      = null);

public record CheckoutResultDto(string OrderId, bool Success, string? Error);
