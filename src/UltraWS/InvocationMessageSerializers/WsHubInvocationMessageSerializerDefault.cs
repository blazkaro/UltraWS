using System.Text.Json;
using UltraWS.Internal.HubMethods;
using UltraWS.Models;

namespace UltraWS.InvocationMessageSerializers;

internal class WsHubInvocationMessageSerializerDefault<THub> : IWsHubInvocationMessageSerializer<THub>
    where THub : WsHub<THub>
{
    private readonly IWsHubMethodStore<THub> _methodStore;

    public WsHubInvocationMessageSerializerDefault(IWsHubMethodStore<THub> methodStore)
    {
        _methodStore = methodStore;
    }

    public WsHubInvocationMessage? Deserialize(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
    {
        var result = new WsHubInvocationMessage();
        var reader = new Utf8JsonReader(message.Span);
        try
        {
            if (!JsonElement.TryParseValue(ref reader, out JsonElement? invocationMessage) || invocationMessage.Value.ValueKind != JsonValueKind.Object)
                return null;

            // Read method name
            if (invocationMessage.Value.TryGetProperty("MethodName", out JsonElement methodName) && methodName.ValueKind == JsonValueKind.String)
            {
                result.MethodName = methodName.ToString();
            }
            else
            {
                return null;
            }

            // Check if method exists in hub
            if (!_methodStore.Exists(result.MethodName))
                return null;

            // Read args
            if (invocationMessage.Value.TryGetProperty("Args", out JsonElement args) && args.ValueKind == JsonValueKind.Array)
            {
                var argTypes = _methodStore.GetArgsTypes(result.MethodName);
                result.Args = new object[argTypes.Count];

                // If args count doesn't match immediately return null because we won't determine which argument is omitted or extra
                if (args.GetArrayLength() != argTypes.Count)
                    return null;

                for (int i = 0; i < argTypes.Count; i++)
                {
                    result.Args[i] = args[i].Deserialize(argTypes[i], new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
        }
        catch
        {
            // Catch when any exception is thrown. We want to return only fully valid messages
            return null;
        }

        return result;
    }

    public byte[] Serialize(WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message);
    }
}
