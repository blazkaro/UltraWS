namespace UltraWS;

public class WsHubOptions<THub>
    where THub : WsHub<THub>
{
    /// <summary>
    /// Buffer size in bytes. Default to 32768 bytes, so 32kB
    /// </summary>
    public int BufferSize { get; set; } = 32768;

    /// <summary>
    /// Anonymous client identifier size in bytes. Default to 32 bytes
    /// </summary>
    public int AnonymousClientIdSize { get; set; } = 32;

    /// <summary>
    /// The client type
    /// </summary>
    public WsClientType ClientType { get; set; } = WsClientType.UserWhenAuthenticated;

    /// <summary>
    /// Whether the hello message should be sent. If <c>true</c>, then after client is connected the HelloMessage with clientId will be sent immediately.
    /// Otherwise, no message will be sent when client connects. Default to <c>false</c>
    /// </summary>
    public bool HelloMessageShouldBeSent { get; set; } = false;
}
