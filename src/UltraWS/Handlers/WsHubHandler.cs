using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using UltraWS.Exceptions;
using UltraWS.InvocationMessageSerializers;
using UltraWS.Managers;
using UltraWS.Models;
using UltraWS.Proxies.Default;
using UltraWS.Services;

namespace UltraWS.Handlers;

public class WsHubHandler<THub>
    where THub : WsHub<THub>
{
    private readonly THub _hub;
    private readonly IWsHubManager<THub> _hubManager;
    private readonly WsHubOptions<THub> _hubOptions;
    private readonly IWsHubInvocationMessageSerializer<THub> _invocationMessageSerializer;
    private readonly IWsHubUserIdProvider<THub> _userIdProvider;
    private readonly HttpContext _httpContext;

    public WsHubHandler(THub hub,
        IWsHubManager<THub> hubManager,
        IOptions<WsHubOptions<THub>> hubOptions,
        IWsHubInvocationMessageSerializer<THub> invocationMessageSerializer,
        IWsHubUserIdProvider<THub> userIdProvider,
        IHttpContextAccessor contextAccessor)
    {
        _hub = hub;
        _hubManager = hubManager;
        _hubOptions = hubOptions.Value;
        _invocationMessageSerializer = invocationMessageSerializer;
        _userIdProvider = userIdProvider;
        _httpContext = contextAccessor.HttpContext ?? throw new NullHttpContextException();
    }

    public async Task HandleAsync()
    {
        if (_httpContext.WebSockets.IsWebSocketRequest)
        {
            WebSocket? webSocket = null;
            WebSocketCloseStatus? closeStatus = null;

            try
            {
                webSocket = await _httpContext.WebSockets.AcceptWebSocketAsync();

                string? clientId;
                var userId = _userIdProvider.GetUserId(_httpContext.User);
                if (_hubOptions.ClientType == WsClientType.UserWhenAuthenticated && !string.IsNullOrEmpty(userId))
                {
                    clientId = userId;
                }
                else
                {
                    clientId = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(_hubOptions.AnonymousClientIdSize));
                }

                InitializeHub(clientId, webSocket, _httpContext.User);
                await Task.WhenAll(_hubManager.OnConnectedAsync(_hub.Context), _hub.OnConnectedAsync());

                if (_hubOptions.HelloMessageShouldBeSent)
                    await SendHelloMessageAsync(clientId, _httpContext.RequestAborted);

                closeStatus = await ReceiveAsync(webSocket, _httpContext.RequestAborted);
            }
            catch
            {
                // The finally block will clean up
            }
            finally
            {
                if (_hub.Initialized)
                    await Task.WhenAll(_hubManager.OnDisconnectedAsync(_hub.Context), _hub.OnDisconnectedAsync());

                try
                {
                    if (webSocket is not null && webSocket.State == WebSocketState.Open && closeStatus.HasValue)
                    {
                        // Request was bad (malformed etc) so we send close

                        await webSocket.CloseAsync(closeStatus.Value, null, _httpContext.RequestAborted);
                    }
                    else if (webSocket is not null && (webSocket.State == WebSocketState.Closed || webSocket.State == WebSocketState.CloseReceived))
                    {
                        // We respond to requested close. Everything was OK
                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, _httpContext.RequestAborted);
                    }
                }
                catch
                {
                    // Nothing to do, the client is not going to complete handshake
                }

                webSocket?.Dispose();
            }
        }
        else
        {
            _httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task<WebSocketCloseStatus?> ReceiveAsync(WebSocket ws, CancellationToken cancellationToken)
    {
        var buffer = WebSocket.CreateServerBuffer(_hubOptions.BufferSize);
        try
        {
            while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                int receivedBytes = 0;
                WebSocketReceiveResult receiveResult;
                do
                {
                    receiveResult = await ws.ReceiveAsync(buffer, cancellationToken);
                    receivedBytes += receiveResult.Count;
                } while (!receiveResult.EndOfMessage && receivedBytes < _hubOptions.BufferSize && !cancellationToken.IsCancellationRequested);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                    return WebSocketCloseStatus.NormalClosure;

                if (receivedBytes >= _hubOptions.BufferSize)
                    return WebSocketCloseStatus.MessageTooBig;

                if (receiveResult.EndOfMessage)
                {
                    var message = _invocationMessageSerializer.Deserialize(buffer.AsMemory(0, receivedBytes), cancellationToken);
                    if (message is null)
                        return WebSocketCloseStatus.InvalidPayloadData;
                    
                    await _hub.HandleAsync(message, cancellationToken);
                    buffer = WebSocket.CreateServerBuffer(_hubOptions.BufferSize);
                }
            }
        }
        catch
        {
            // Do nothing here, we will just return null in case of any exception (task canceled exception or connection closed without handshake)
        }

        // This will happen only when exception is thrown. We return here to satisfy compiler (all possible paths returns)
        // In any other case the appropriate close status will be returned before this statement
        return null;
    }

    private void InitializeHub(string clientId, WebSocket webSocket, ClaimsPrincipal? user)
    {
        _hub.Context = new WsHubContext(clientId, new WsHubConnection(webSocket), user);
        _hub.Clients = new ClientProxyDefault<THub>(_hubManager);
        _hub.Groups = new GroupProxyDefault<THub>(_hubManager);
        _hub.Initialized = true;
    }

    private async Task SendHelloMessageAsync(string clientId, CancellationToken cancellationToken)
    {
        var serialized = _invocationMessageSerializer.Serialize(new WsHubHelloMessage(clientId), cancellationToken);
        await _hub.Context.Connection.SendAsync(serialized, cancellationToken);
    }
}
