namespace UcpAgent.SharedKernel.Ports;

public interface IWebhookEventPort
{
    /// <summary>
    /// Verifica idempotência e persiste o evento.
    /// Retorna true se é novo (deve processar) ou false se duplicado (ignorar).
    /// </summary>
    Task<bool> TryRecordAsync(string provider, string externalId,
        string eventType, string payloadJson, CancellationToken ct = default);
}
