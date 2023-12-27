using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using UltraWS.Exceptions;

namespace UltraWS.Internal.HubMethods;

internal class WsHubMethodStore<THub> : IWsHubMethodStore<THub>
    where THub : WsHub<THub>
{
    private readonly ImmutableDictionary<string, ImmutableList<Type>> _methodArgs;

    public WsHubMethodStore(ImmutableDictionary<string, ImmutableList<Type>> methodArgs)
    {
        _methodArgs = methodArgs;
    }

    public bool Exists([NotNullWhen(true)] string? methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            return false;

        return _methodArgs.ContainsKey(methodName);
    }

    public IImmutableList<Type> GetArgsTypes(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
            throw new ArgumentException($"'{nameof(methodName)}' cannot be null or empty.", nameof(methodName));

        if (!Exists(methodName))
            throw new HubMethodException($"The method '{methodName}' doesn't exist on this hub");

        return _methodArgs[methodName];
    }
}
