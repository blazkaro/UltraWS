using System.Security.Claims;

namespace UltraWS.Models;

public class WsHubContext
{
    public WsHubContext(string clientId, WsHubConnection connection, ClaimsPrincipal? user)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentException($"'{nameof(clientId)}' cannot be null or empty.", nameof(clientId));

        ClientId = clientId;
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        User = user;
    }

    public string ClientId { get; set; }
    public WsHubConnection Connection { get; set; }
    public ClaimsPrincipal? User { get; set; }
}
