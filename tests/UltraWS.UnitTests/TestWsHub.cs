using UltraWS.Models;

namespace UltraWS.UnitTests;

public class TestWsHub : WsHub<TestWsHub>
{
    public override Task HandleAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
