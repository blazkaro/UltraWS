using UltraWS.Models;

namespace UltraWS.Proxies;

public interface IClientProxy
{
    Task SendAsync(string clientId, WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    Task SendAsync(IReadOnlyCollection<string> clientId, WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    Task SendAllAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    Task<bool> IsInGroup(string clientId, string groupId, CancellationToken cancellationToken = default);
}
