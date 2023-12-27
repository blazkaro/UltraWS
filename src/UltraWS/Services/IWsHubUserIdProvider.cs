using System.Security.Claims;

namespace UltraWS.Services;

public interface IWsHubUserIdProvider<THub>
    where THub : WsHub<THub>
{
    string? GetUserId(ClaimsPrincipal? principal);
}
