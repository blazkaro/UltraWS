# UltraWS

Simple, easy to use WebSocket server, similar to SignalR. 

The key difference is that SignalR operates on WebSocket connections, while this implementation uses concept of **client**, which can be single connection (then it works like SignalR), or the authenticated entity, and then WebSocket messages are synchronized over all client connections.

## Sample usage:

### Program.cs
```csharp
services.AddUltraWs()
    .AddWsHub<ChatHub>(new WsHubOptions<ChatHub>
    {
    });

...

app.UseWebSockets();
app.MapUltraWs<ChatHub>("");
```

The `MapUltraWs()` method is Minimal API builder, so you can add authentication, authorization, filters etc. easily.

### Hub
Like SignalR, this implementation relies on concept of `Hub`. You create it by inheriting from `WsHub<Thub>`, like it:

```csharp
public class ChatHub : WsHub<ChatHub>
{
    public override async Task HandleAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        switch (message.MethodName)
        {
            case nameof(SendPrivateMessage):
                await SendPrivateMessage(message.Args[0] as SendPrivateMessageDto);
                break;
        }
    }

    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync()
    {
        return base.OnDisconnectedAsync();
    }

    public async Task SendPrivateMessage(SendPrivateMessageDto dto)
    {
        var id = Context.ClientId;
        await Clients.SendAsync(id, new WsHubInvocationMessage
        {
            MethodName = "ReceivePrivateMessage",
            Args = new object[]
            {
                dto
            }
        });
    }
}
```

The key difference is that you have to implement `HandleAsync()` method as shown in the code above. You need to switch by `message.MethodName` and invoke appropriate method. It's done this way to avoid too much reflection.