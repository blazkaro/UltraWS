using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UltraWS.Handlers;

namespace UltraWS.Extensions;

public static class WebAppExtensions
{
    public static RouteHandlerBuilder MapUltraWs<THub>(this IEndpointRouteBuilder endpoints, PathString path)
        where THub : WsHub<THub>
    {
        return endpoints.Map(path, async (WsHubHandler<THub> handler) => await handler.HandleAsync());
    }
}
