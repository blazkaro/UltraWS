using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using UltraWS.Extensions;
using UltraWS.IntegrationTests.Fixtures.Base;

namespace UltraWS.IntegrationTests.Fixtures;

public class WsHubEndpoint_WithoutHelloMessageFixture : WsHubEndpointFixtureBase
{
    protected override TestServer Server { get; init; }
    public override Uri WsHubUri { get; protected init; }

    public WsHubEndpoint_WithoutHelloMessageFixture()
    {
        Server = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(Environment.CurrentDirectory);

                builder.ConfigureServices(services =>
                {
                    services.AddUltraWs()
                        .AddWsHub<TestWsHub>(new WsHubOptions<TestWsHub>
                        {
                            HelloMessageShouldBeSent = false
                        });
                });

                builder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseWebSockets();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapUltraWs<TestWsHub>("")
                            .AllowAnonymous();
                    });
                });
            }).Server;

        WsHubUri = Server.BaseAddress;
    }
}