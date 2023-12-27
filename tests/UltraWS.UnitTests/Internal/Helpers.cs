using System.Net.WebSockets;
using UltraWS.Models;

namespace UltraWS.UnitTests.Internal;

internal static class Helpers
{
    internal static WsHubConnection CreateFakeClientConnection()
    {
        var fakeWebSocket = WebSocket.CreateFromStream(new MemoryStream(), new WebSocketCreationOptions());
        return new WsHubConnection(fakeWebSocket);
    }
}
