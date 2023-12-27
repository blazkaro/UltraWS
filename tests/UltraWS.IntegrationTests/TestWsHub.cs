using UltraWS.IntegrationTests.Dtos;
using UltraWS.Models;

namespace UltraWS.IntegrationTests;

internal class TestWsHub : WsHub<TestWsHub>
{
    public override async Task HandleAsync(WsHubInvocationMessage message, CancellationToken cancellationToken = default)
    {
        switch (message.MethodName)
        {
            case nameof(SendMessage): await SendMessage(message.Args[0] as SendMessageDto); break;
            case nameof(JoinGroup): await JoinGroup(message.Args[0] as string); break;
            case nameof(SendMessageToGroup): await SendMessageToGroup(message.Args[0] as SendGroupMessageDto); break;
        }
    }

    public async Task SendMessage(SendMessageDto sendMessageDto)
    {
        await Clients.SendAsync(sendMessageDto.TargetClientId, new WsHubInvocationMessage
        {
            MethodName = "ReceiveMessage",
            Args = new object[]
            {
                new ReceiveMessageDto(sendMessageDto.Content)
            }
        });
    }

    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ClientId, groupId);
    }

    public async Task SendMessageToGroup(SendGroupMessageDto sendGroupMessageDto)
    {
        await Groups.SendAsync(sendGroupMessageDto.GroupId, new WsHubInvocationMessage
        {
            MethodName = "ReceiveGroupMessage",
            Args = new object[]
            {
                new ReceiveMessageDto(sendGroupMessageDto.Content)
            }
        });
    }
}
