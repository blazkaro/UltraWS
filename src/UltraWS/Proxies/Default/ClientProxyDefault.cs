using UltraWS.Managers;
using UltraWS.Models;

namespace UltraWS.Proxies.Default;

internal class ClientProxyDefault<THub> : IClientProxy
    where THub : WsHub<THub>
{
    private readonly IWsHubManager<THub> _hubManager;

    public ClientProxyDefault(IWsHubManager<THub> hubManager)
    {
        _hubManager = hubManager;
    }

    public Task<bool> IsInGroup(string clientId, string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        return _hubManager.IsInGroup(clientId, groupId, cancellationToken);
    }

    public Task SendAllAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        return _hubManager.SendAllAsync(message, cancellationToken);
    }

    public Task SendAsync(string clientId, WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        return _hubManager.SendAsync(clientId, message, cancellationToken);
    }

    public Task SendAsync(IReadOnlyCollection<string> clientId, WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (clientId is null)
            throw new ArgumentNullException(nameof(clientId));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        return _hubManager.SendAsync(clientId, message, cancellationToken);
    }
}
