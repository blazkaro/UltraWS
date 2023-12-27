using ConcurrentCollections;
using System.Collections.Concurrent;

namespace UltraWS.Managers.Default.Internal;

public class WsHubManagerDefaultStore<THub>
    where THub : WsHub<THub>
{
    /// <summary>
    /// <para>Key: the id of client</para>
    /// <para>Value: The WsClient</para>
    /// </summary>
    public readonly ConcurrentDictionary<string, WsClient> Clients = new();

    /// <summary>
    /// <para>Key: the id of group</para>
    /// <para>Value: The set of member ids (client's ids)</para>
    /// </summary>
    public readonly ConcurrentDictionary<string, ConcurrentHashSet<string>> Groups = new();
}
