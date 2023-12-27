using UltraWS.Managers;
using UltraWS.Models;

namespace UltraWS.Proxies.Default;

internal class GroupProxyDefault<THub> : IGroupProxy
    where THub : WsHub<THub>
{
    private readonly IWsHubManager<THub> _hubManager;

    public GroupProxyDefault(IWsHubManager<THub> hubManager)
    {
        _hubManager = hubManager;
    }

    public Task AddToGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        return _hubManager.AddToGroupAsync(clientId, groupId, cancellationToken);
    }

    public Task AddToGroupAsync(string clientId, IReadOnlyCollection<string> groupIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (groupIds is null)
            throw new ArgumentNullException(nameof(groupIds));

        return _hubManager.AddToGroupAsync(clientId, groupIds, cancellationToken);
    }

    public Task RemoveFromGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        return _hubManager.RemoveFromGroupAsync(clientId, groupId, cancellationToken);
    }

    public Task SendAsync(string groupId, WsHubInvocationMessage wsHubInvocationMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        if (wsHubInvocationMessage is null)
            throw new ArgumentNullException(nameof(wsHubInvocationMessage));

        return _hubManager.SendGroupAsync(groupId, wsHubInvocationMessage, cancellationToken);
    }

    public Task SendAsync(IReadOnlyCollection<string> groupIds, WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (groupIds is null)
            throw new ArgumentNullException(nameof(groupIds));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        return _hubManager.SendGroupAsync(groupIds, message, cancellationToken);
    }
}
