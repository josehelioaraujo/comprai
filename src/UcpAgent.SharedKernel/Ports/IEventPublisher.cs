namespace UcpAgent.SharedKernel.Ports;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T payload, CancellationToken ct = default) where T : class;
}
