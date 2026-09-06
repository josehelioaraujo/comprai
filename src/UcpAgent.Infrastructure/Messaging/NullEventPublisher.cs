using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Messaging;

public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(string topic, T payload, CancellationToken ct = default) where T : class
        => Task.CompletedTask;
}
