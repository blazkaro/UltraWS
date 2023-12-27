using ConcurrentCollections;
using System.Collections.ObjectModel;
using UltraWS.InvocationMessageSerializers;
using UltraWS.Managers.Default.Internal;
using UltraWS.Models;

namespace UltraWS.Managers.Default;

public class WsHubManagerDefault<THub> : IWsHubManager<THub>
    where THub : WsHub<THub>
{
    private readonly WsHubManagerDefaultStore<THub> _store;
    private readonly IWsHubInvocationMessageSerializer<THub> _invocationMessageSerializer;

    public WsHubManagerDefault(WsHubManagerDefaultStore<THub> store, IWsHubInvocationMessageSerializer<THub> invocationMessageSerializer)
    {
        _store = store;
        _invocationMessageSerializer = invocationMessageSerializer;
    }

    public Task AddToGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        AddClientToGroupInternal(clientId, groupId);

        return Task.CompletedTask;
    }

    public Task AddToGroupAsync(string clientId, IReadOnlyCollection<string> groupIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (groupIds is null)
            throw new ArgumentNullException(nameof(groupIds));

        if (!_store.Clients.ContainsKey(clientId))
            return Task.CompletedTask;

        foreach (var groupId in groupIds)
        {
            AddClientToGroupInternal(clientId, groupId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Assumes that client exists to avoid multiple checks for the same client.
    /// Do the check beyond this internal method
    /// </summary>
    private void AddClientToGroupInternal(string clientId, string groupId)
    {
        if (!_store.Clients.TryGetValue(clientId, out WsClient? client) || client is null)
            return;

        client.GroupIds.Add(groupId);

        if (!_store.Groups.TryAdd(groupId, new ConcurrentHashSet<string> { clientId }))
        {
            _store.Groups[groupId].Add(clientId);
        }
    }

    public Task OnConnectedAsync(WsHubContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var wsClient = new WsClient
        {
            Connections = new() { context.Connection }
        };

        if (!_store.Clients.TryAdd(context.ClientId, wsClient))
        {
            _store.Clients[context.ClientId].Connections.Add(context.Connection);
        }

        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(WsHubContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (_store.Clients.TryGetValue(context.ClientId, out WsClient? client) && client is not null)
        {
            // If we want to remove client from groups on disconnect, we need to ensure that no more than 1 connection is active.
            // It's because for instance we don't want to remove clients from all groups if user disconnects at mobile device, but is still active at PC.
            var tasks = new List<Task>(client.GroupIds.Count);
            if (client.Connections.Count < 2)
            {
                foreach (var groupId in client.GroupIds)
                {
                    if (!_store.Groups.ContainsKey(groupId))
                        continue;

                    tasks.Add(RemoveFromGroupAsync(context.ClientId, groupId));
                }
            }

            if (client.Connections.TryRemove(context.Connection) && client.Connections.IsEmpty)
                _store.Clients.TryRemove(context.ClientId, out _);

            return Task.WhenAll(tasks);
        }

        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string clientId, string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        if (_store.Clients.TryGetValue(clientId, out WsClient? client) && client is not null)
        {
            client.GroupIds.TryRemove(groupId);
        }

        if (_store.Groups.TryGetValue(groupId, out ConcurrentHashSet<string>? group) && group.TryRemove(clientId) && group.IsEmpty)
            _store.Groups.TryRemove(groupId, out _);

        return Task.CompletedTask;
    }

    public Task SendAllAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        return SendAsync((ReadOnlyCollection<string>)_store.Clients.Keys, message, cancellationToken);
    }

    public Task SendAsync(string clientId, WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var serializedMsg = _invocationMessageSerializer.Serialize(message, cancellationToken);
        return SendAsyncInternal(clientId, serializedMsg, cancellationToken);
    }

    public Task SendAsync(IReadOnlyCollection<string> clientIds, WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (clientIds is null)
            throw new ArgumentNullException(nameof(clientIds));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var serializedMsg = _invocationMessageSerializer.Serialize(message, cancellationToken);

        var tasks = new List<Task>(clientIds.Count);
        foreach (var clientId in clientIds)
        {
            tasks.Add(SendAsyncInternal(clientId, serializedMsg, cancellationToken));
        }

        return Task.WhenAll(tasks);
    }

    private Task SendAsyncInternal(string clientId, byte[] message, CancellationToken cancellationToken)
    {
        if (!_store.Clients.TryGetValue(clientId, out WsClient? client) || client is null)
            return Task.CompletedTask;

        if (client.Connections.Count > 0)
        {
            var tasks = new List<Task>(client.Connections.Count);
            foreach (var conn in client.Connections)
            {
                tasks.Add(conn.SendAsync(message, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        return Task.CompletedTask;
    }

    public Task SendGroupAsync(string groupId, WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (!_store.Groups.TryGetValue(groupId, out ConcurrentHashSet<string>? group) || group is null)
            return Task.CompletedTask;

        return SendAsync(group, message, cancellationToken);
    }

    public Task SendGroupAsync(IReadOnlyCollection<string> groupIds, WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        if (groupIds is null)
            throw new ArgumentNullException(nameof(groupIds));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var serializedMsg = _invocationMessageSerializer.Serialize(message, cancellationToken);
        var tasks = new List<Task>(groupIds.Count);
        foreach (var groupId in groupIds)
        {
            if (!_store.Groups.TryGetValue(groupId, out ConcurrentHashSet<string>? group) || group is null)
                return Task.CompletedTask;

            var clientSendTasks = new List<Task>(group.Count);
            foreach (var clientId in group)
            {
                clientSendTasks.Add(SendAsyncInternal(clientId, serializedMsg, cancellationToken));
            }

            tasks.Add(Task.WhenAll(clientSendTasks));
        }

        return Task.WhenAll(tasks);
    }

    public Task<bool> IsInGroup(string clientId, string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        if (string.IsNullOrEmpty(groupId))
            throw new ArgumentException($"'{nameof(groupId)}' cannot be null or empty.", nameof(groupId));

        return Task.FromResult(_store.Groups.TryGetValue(groupId, out ConcurrentHashSet<string>? group)
            && group is not null
            && group.Contains(clientId));
    }
}
