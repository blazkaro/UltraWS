using UltraWS.Models;

namespace UltraWS.Managers;

public interface IWsHubManager<THub>
    where THub : WsHub<THub>
{
    Task OnConnectedAsync(WsHubContext context);
    Task OnDisconnectedAsync(WsHubContext context);

    Task SendAsync(string clientId, WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    Task SendAsync(IReadOnlyCollection<string> clientIds, WsHubInvocationMessage message, CancellationToken cancellationToken = default);

    Task SendGroupAsync(string groupId, WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    Task SendGroupAsync(IReadOnlyCollection<string> groupIds, WsHubInvocationMessage message, CancellationToken cancellationToken = default);
    Task SendAllAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default);

    Task AddToGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default);
    Task AddToGroupAsync(string clientId, IReadOnlyCollection<string> groupIds, CancellationToken cancellationToken = default);
    Task RemoveFromGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default);

    Task<bool> IsInGroup(string clientId, string groupId, CancellationToken cancellationToken = default);
}
