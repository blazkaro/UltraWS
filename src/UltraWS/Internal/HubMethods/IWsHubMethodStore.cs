using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace UltraWS.Internal.HubMethods;

internal interface IWsHubMethodStore<THub>
    where THub : WsHub<THub>
{
    bool Exists([NotNullWhen(true)] string? methodName);
    IImmutableList<Type> GetArgsTypes(string methodName);
}
