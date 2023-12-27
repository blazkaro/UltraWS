using Microsoft.AspNetCore.TestHost;

namespace UltraWS.IntegrationTests.Fixtures.Base;

public abstract class WsHubEndpointFixtureBase : IDisposable
{
    protected abstract TestServer Server { get; init; }
    public abstract Uri WsHubUri { get; protected init; }

    public HttpClient GetHttpClient() => Server.CreateClient();
    public WebSocketClient GetWebSocketClient() => Server.CreateWebSocketClient();

    public void Dispose()
    {
        Server?.Dispose();
    }
}
