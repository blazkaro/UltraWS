using System.Net.WebSockets;

namespace UltraWS.Models;

public class WsHubConnection
{
    private readonly WebSocket _webSocket;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    public WsHubConnection(WebSocket webSocket)
    {
        if (webSocket is null)
            throw new ArgumentNullException(nameof(webSocket));

        if (webSocket.State != WebSocketState.Open)
            throw new WebSocketException("The connection is not open");

        _webSocket = webSocket;
    }

    public async Task SendAsync(byte[] message, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        if (_webSocket.State != WebSocketState.Open)
            throw new WebSocketException("The connection is not open");

        await _webSocket.SendAsync(message, WebSocketMessageType.Binary, true, cancellationToken);

        _semaphore.Release();
    }
}
