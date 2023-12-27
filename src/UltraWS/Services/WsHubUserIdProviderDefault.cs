using System.Security.Claims;

namespace UltraWS.Services;

public class WsHubUserIdProviderDefault<THub> : IWsHubUserIdProvider<THub>
    where THub : WsHub<THub>
{
    public string? GetUserId(ClaimsPrincipal? principal)
    {
        if (principal is null)
            return null;

        return principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
