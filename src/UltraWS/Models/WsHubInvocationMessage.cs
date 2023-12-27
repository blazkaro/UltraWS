namespace UltraWS.Models;

public class WsHubInvocationMessage
{
    public string MethodName { get; set; }
    public object?[]? Args { get; set; }
}
