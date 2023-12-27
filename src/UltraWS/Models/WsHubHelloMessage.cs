namespace UltraWS.Models;

public class WsHubHelloMessage : WsHubInvocationMessage
{
    private WsHubHelloMessage() { }

    public WsHubHelloMessage(string clientId)
    {
        MethodName = "Hello";
        Args = new object[]
        {
            new
            {
                ClientId = clientId
            }
        };
    }
}
