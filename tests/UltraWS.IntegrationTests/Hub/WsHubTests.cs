using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using UltraWS.IntegrationTests.Dtos;
using UltraWS.IntegrationTests.Fixtures;
using UltraWS.Models;

namespace UltraWS.IntegrationTests.Hub;

public class WsHubTests : IClassFixture<WsHubEndpoint_WithHelloMessageFixture>, IClassFixture<WsHubEndpoint_WithAuthorizationFixture>,
    IClassFixture<WsHubEndpoint_WithoutHelloMessageFixture>
{
    private readonly WsHubEndpoint_WithHelloMessageFixture _wsHubWithHelloMessageFixture;
    private readonly WsHubEndpoint_WithoutHelloMessageFixture _wsHubWithoutHelloMessageFixture;
    private readonly WsHubEndpoint_WithAuthorizationFixture _wsHubWithAuthorizationFixture;

    private const int WS_RECEIVE_BUFFER_SIZE = 65536;
    private const int WS_SEND_BUFFER_SIZE = 65536;

    /// <summary>
    /// This time is purely magic number. Just we assume that without network latency the hello message won't come after one second
    /// </summary>
    private readonly TimeSpan TIMEOUT_AFTER = TimeSpan.FromSeconds(1);

    public WsHubTests(WsHubEndpoint_WithHelloMessageFixture wsHubWithHelloMessageFixture,
        WsHubEndpoint_WithoutHelloMessageFixture wsHubWithoutHelloMessageFixture,
        WsHubEndpoint_WithAuthorizationFixture wsHubWithAuthorizationFixture)
    {
        _wsHubWithHelloMessageFixture = wsHubWithHelloMessageFixture;
        _wsHubWithoutHelloMessageFixture = wsHubWithoutHelloMessageFixture;
        _wsHubWithAuthorizationFixture = wsHubWithAuthorizationFixture;
    }

    [Fact]
    public async Task When_ClientSentConnectRequest_And_RequestIsNotWebSocket_Then_Returns_400BadRequest()
    {
        var result = await _wsHubWithHelloMessageFixture.GetHttpClient().GetAsync(_wsHubWithHelloMessageFixture.WsHubUri);

        result.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task When_ClientConnects_And_HubIsConfiguredToSendHelloMessage_Then_ShouldReceiveHelloMessage()
    {
        var ws = await _wsHubWithHelloMessageFixture.GetWebSocketClient().ConnectAsync(_wsHubWithHelloMessageFixture.WsHubUri, CancellationToken.None);

        var buffer = WebSocket.CreateClientBuffer(WS_RECEIVE_BUFFER_SIZE, WS_SEND_BUFFER_SIZE);
        var received = await ws.ReceiveAsync(buffer, CancellationToken.None);

        var helloMessage = JsonSerializer.Deserialize<WsHubInvocationMessage>(buffer.AsSpan(0, received.Count));
        var clientId = (helloMessage.Args[0] as JsonElement?).Value.GetProperty("ClientId").GetString();

        helloMessage.Should().NotBeNull();
        helloMessage.Args.Should().HaveCountGreaterThan(0);
        clientId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task When_ClientsConnects_And_HubIsConfiguredToNotSendHelloMessage_Then_NothingShouldBeReceived()
    {
        var ws = await _wsHubWithoutHelloMessageFixture.GetWebSocketClient().ConnectAsync(_wsHubWithoutHelloMessageFixture.WsHubUri, CancellationToken.None);

        var buffer = WebSocket.CreateClientBuffer(WS_RECEIVE_BUFFER_SIZE, WS_SEND_BUFFER_SIZE);

        var received = ws.ReceiveAsync(buffer, CancellationToken.None);
        var delay = Task.Delay(TIMEOUT_AFTER);

        var completed = await Task.WhenAny(received, delay);

        // It means that delay task (delayed by time after which time for hello message is exceeded) is completed before receiving hello message
        completed.Should().Be(delay);
    }

    [Fact]
    public async Task When_HubUsesUserAsClientWhenPossibleAndClientIsAuthenticatedAndHasMultipleConnections_Then_MessageToClientIdIsSentOverAllConnections()
    {
        const string userId = "this-is-user-id-which-will-be-used-as-client-id-if-request-is-authenticated-and-its-also-due-to-ws-client-type-configuration";
        var wsClient = _wsHubWithAuthorizationFixture.GetWebSocketClient();
        var wsClient2 = _wsHubWithAuthorizationFixture.GetWebSocketClient();

        var authCookie = await _wsHubWithAuthorizationFixture.GetAuthenticationCookie(userId);
        void configureRequest(HttpRequest req)
        {
            req.Headers.Cookie = authCookie;
        }

        wsClient.ConfigureRequest = configureRequest;
        wsClient2.ConfigureRequest = configureRequest;

        var ws1 = await wsClient.ConnectAsync(_wsHubWithAuthorizationFixture.WsHubUri, CancellationToken.None);
        var ws2 = await wsClient2.ConnectAsync(_wsHubWithAuthorizationFixture.WsHubUri, CancellationToken.None);

        var ws1Buffer = WebSocket.CreateClientBuffer(WS_RECEIVE_BUFFER_SIZE, WS_RECEIVE_BUFFER_SIZE);
        var ws2Buffer = WebSocket.CreateClientBuffer(WS_RECEIVE_BUFFER_SIZE, WS_RECEIVE_BUFFER_SIZE);

        await ws1.SendAsync(CreateMessageToTargetClient(userId), WebSocketMessageType.Binary, true,
            CancellationToken.None);

        var ws1Received = await ws1.ReceiveAsync(ws1Buffer, CancellationToken.None);
        await ws2.ReceiveAsync(ws2Buffer, CancellationToken.None);

        var deserialized = JsonSerializer.Deserialize<WsHubInvocationMessage>(ws1Buffer.AsSpan(0, ws1Received.Count));

        deserialized.MethodName.Should().Be("ReceiveMessage");
        ws1Buffer.SequenceEqual(ws2Buffer).Should().BeTrue();
    }

    [Fact]
    public async Task When_MessageIsSendToGroup_Then_AllMembersReceiveMessage()
    {
        const string groupId = "group1";

        var wsClient = _wsHubWithoutHelloMessageFixture.GetWebSocketClient();
        var wsClient2 = _wsHubWithoutHelloMessageFixture.GetWebSocketClient();
        var wsClient3 = _wsHubWithoutHelloMessageFixture.GetWebSocketClient();

        var ws1 = await wsClient.ConnectAsync(_wsHubWithoutHelloMessageFixture.WsHubUri, CancellationToken.None);
        var ws2 = await wsClient2.ConnectAsync(_wsHubWithoutHelloMessageFixture.WsHubUri, CancellationToken.None);
        var ws3 = await wsClient3.ConnectAsync(_wsHubWithoutHelloMessageFixture.WsHubUri, CancellationToken.None);

        var ws1Buffer = WebSocket.CreateClientBuffer(WS_RECEIVE_BUFFER_SIZE, WS_RECEIVE_BUFFER_SIZE);
        var ws2Buffer = WebSocket.CreateClientBuffer(WS_RECEIVE_BUFFER_SIZE, WS_RECEIVE_BUFFER_SIZE);
        var ws3Buffer = WebSocket.CreateClientBuffer(WS_RECEIVE_BUFFER_SIZE, WS_RECEIVE_BUFFER_SIZE);

        var joinGroupMsg = CreateJoinGroupMessage(groupId);
        async Task sendJoinGroupInvocation(WebSocket ws)
        {
            await ws.SendAsync(joinGroupMsg, WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        await Task.WhenAll(
            sendJoinGroupInvocation(ws1),
            sendJoinGroupInvocation(ws2),
            sendJoinGroupInvocation(ws3));

        await ws1.SendAsync(CreateMessageToTargetGroup(groupId), WebSocketMessageType.Binary, true, CancellationToken.None);

        var delays = new Task[] { Task.Delay(TIMEOUT_AFTER), Task.Delay(TIMEOUT_AFTER), Task.Delay(TIMEOUT_AFTER) };
        var ws1ReceivedTask = await Task.WhenAny(ws1.ReceiveAsync(ws1Buffer, CancellationToken.None), delays[0]);
        var ws2ReceivedTask = await Task.WhenAny(ws2.ReceiveAsync(ws2Buffer, CancellationToken.None), delays[1]);
        var ws3ReceivedTask = await Task.WhenAny(ws3.ReceiveAsync(ws3Buffer, CancellationToken.None), delays[2]);

        // Every client received message
        ws1ReceivedTask.Should().NotBe(delays[0]);
        ws2ReceivedTask.Should().NotBe(delays[1]);
        ws3ReceivedTask.Should().NotBe(delays[2]);

        // Every client received the same message
        var expectedMessage = ws1Buffer.TakeWhile(p => p != 0);
        expectedMessage.TakeWhile(p => p != 0).Should().BeEquivalentTo(ws2Buffer.TakeWhile(p => p != 0));
        expectedMessage.Should().BeEquivalentTo(ws3Buffer.TakeWhile(p => p != 0));
    }

    private static byte[] CreateMessageToTargetClient(string targetClientId)
    {
        return JsonSerializer.SerializeToUtf8Bytes(new WsHubInvocationMessage
        {
            MethodName = "SendMessage",
            Args = new object[]
            {
                new SendMessageDto(targetClientId, "Message to target client id")
            }
        });
    }

    private static byte[] CreateJoinGroupMessage(string groupId)
    {
        return JsonSerializer.SerializeToUtf8Bytes(new WsHubInvocationMessage
        {
            MethodName = "JoinGroup",
            Args = new object[]
            {
                groupId
            }
        });
    }

    private static byte[] CreateMessageToTargetGroup(string groupId)
    {
        return JsonSerializer.SerializeToUtf8Bytes(new WsHubInvocationMessage
        {
            MethodName = "SendMessageToGroup",
            Args = new object[]
            {
                new SendGroupMessageDto(groupId, "Message to target group")
            }
        });
    }
}
