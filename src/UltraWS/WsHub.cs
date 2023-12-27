using UltraWS.Models;
using UltraWS.Proxies;

namespace UltraWS;

public abstract class WsHub<THub>
    where THub : WsHub<THub>
{
    public IClientProxy Clients { get; internal set; }
    public IGroupProxy Groups { get; internal set; }
    public WsHubContext Context { get; internal set; }

    internal bool Initialized { get; set; }

    public virtual Task OnConnectedAsync() => Task.CompletedTask;
    public virtual Task OnDisconnectedAsync() => Task.CompletedTask;
    public abstract Task HandleAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default);
}