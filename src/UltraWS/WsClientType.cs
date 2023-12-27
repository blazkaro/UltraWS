namespace UltraWS;

/// <summary>
/// WebSocket hub client type
/// </summary>
public enum WsClientType
{
    /// <summary>
    /// It doesn't matter whether connect request is authenticated. 
    /// The new client will be created for every connection and the client id will always be randomly generated.
    /// </summary>
    AlwaysAnonymous,

    /// <summary>
    /// If connect request is authenticated, then the user id will be used as client id.
    /// Otherwise, the new client will be created and the new random client id will be generated.
    /// </summary>
    UserWhenAuthenticated
}