using UltraWS.Models;

namespace UltraWS.InvocationMessageSerializers;

public interface IWsHubInvocationMessageSerializer<THub>
    where THub : WsHub<THub>
{
    byte[] Serialize(WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    WsHubInvocationMessage? Deserialize(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default);
}
