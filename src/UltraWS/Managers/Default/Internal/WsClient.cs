using ConcurrentCollections;
using UltraWS.Models;

namespace UltraWS.Managers.Default.Internal;

public class WsClient
{
    public ConcurrentHashSet<WsHubConnection> Connections { get; init; } = new();
    public ConcurrentHashSet<string> GroupIds { get; init; } = new();
}