namespace UcpAgent.SharedKernel.Ports;

public interface IChannelPort
{
    string ChannelName { get; }
    Task SendMessageAsync(string recipientId, string message, CancellationToken cancellationToken = default);
}
