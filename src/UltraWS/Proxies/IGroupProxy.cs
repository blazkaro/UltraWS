using UltraWS.Models;

namespace UltraWS.Proxies;

public interface IGroupProxy
{
    Task SendAsync(string groupId, WsHubInvocationMessage wsHubInvocationMessage, CancellationToken cancellationToken = default);
    Task SendAsync(IReadOnlyCollection<string> groupIds, WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    Task AddToGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default);
    Task AddToGroupAsync(string clientId, IReadOnlyCollection<string> groupIds, CancellationToken cancellationToken = default);
    Task RemoveFromGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default);
}
