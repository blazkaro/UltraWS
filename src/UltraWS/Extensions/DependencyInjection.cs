using Microsoft.Extensions.DependencyInjection;
using UltraWS.Builders;

namespace UltraWS.Extensions;

public static class DependencyInjection
{
    public static UltraWsBuilder AddUltraWs(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        return new UltraWsBuilder(services);
    }
}
