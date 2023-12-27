using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Immutable;
using System.Reflection;
using UltraWS.Handlers;
using UltraWS.Internal.HubMethods;
using UltraWS.InvocationMessageSerializers;
using UltraWS.Managers;
using UltraWS.Managers.Default;
using UltraWS.Managers.Default.Internal;
using UltraWS.Services;

namespace UltraWS.Builders;

public class UltraWsBuilder
{
    private readonly IServiceCollection _services;

    public UltraWsBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public UltraWsBuilder AddWsHub<THub>(WsHubOptions<THub> config)
        where THub : WsHub<THub>
    {
        var options = Microsoft.Extensions.Options.Options.Create(config);

        _services.TryAddSingleton(options);
        _services.TryAddSingleton<IWsHubUserIdProvider<THub>, WsHubUserIdProviderDefault<THub>>();
        _services.TryAddSingleton<IWsHubManager<THub>, WsHubManagerDefault<THub>>();

        // For default manager implementation, add the store
        if (_services.Any(p => p.ImplementationType == typeof(WsHubManagerDefault<THub>) && p.ServiceType == typeof(IWsHubManager<THub>)))
        {
            _services.TryAddSingleton(new WsHubManagerDefaultStore<THub>());
        }

        _services.TryAddSingleton<IWsHubInvocationMessageSerializer<THub>, WsHubInvocationMessageSerializerDefault<THub>>();

        // Store hub methods to avoid too much reflection later
        var hubMethodArgs = typeof(THub).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.Name != nameof(WsHub<THub>.HandleAsync))
            .ToImmutableDictionary(
            k => k.Name,
            v => v.GetParameters().Select(p => p.ParameterType).ToImmutableList());

        var methodsStore = new WsHubMethodStore<THub>(hubMethodArgs);

        _services.TryAddSingleton<IWsHubMethodStore<THub>>(methodsStore);

        _services.TryAddScoped<THub>();
        _services.TryAddScoped<WsHubHandler<THub>>();

        return this;
    }

    public UltraWsBuilder AddWsHub<THub>()
        where THub : WsHub<THub>
    {
        return AddWsHub(new WsHubOptions<THub>());
    }
}
