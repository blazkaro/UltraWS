using FluentAssertions;
using Moq;
using UltraWS.InvocationMessageSerializers;
using UltraWS.Managers.Default;
using UltraWS.Managers.Default.Internal;
using UltraWS.Models;
using UltraWS.UnitTests.Internal;

namespace UltraWS.UnitTests.HubManagers;

public class WsHubManagerDefaultTests
{
    private readonly WsHubManagerDefaultStore<TestWsHub> _store;
    private readonly WsHubManagerDefault<TestWsHub> _hubManager;

    public WsHubManagerDefaultTests()
    {
        _store = new WsHubManagerDefaultStore<TestWsHub>();

        var invocationSerializerMock = new Mock<IWsHubInvocationMessageSerializer<TestWsHub>>();
        _hubManager = new WsHubManagerDefault<TestWsHub>(_store, invocationSerializerMock.Object);
    }

    [Fact]
    public async Task When_ClientConnects_And_ContextIsNull_Then_Throw()
    {
        var action = async () => await _hubManager.OnConnectedAsync(null);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task When_ClientConnects_Then_ClientIsStored()
    {
        const string clientId = "client1";
        var context = new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null);

        await _hubManager.OnConnectedAsync(context);

        _store.Clients.TryGetValue(clientId, out WsClient? client).Should().BeTrue();
        client.Should().NotBeNull();
        client.Connections.Should().Contain(context.Connection);
    }

    [Theory]
    [InlineData(null, "group1")]
    [InlineData("", "group1")]
    [InlineData("client1", null)]
    [InlineData("client1", "")]
    public async Task When_ClientIsAddedToGroup_And_ClientIdIsNullOrEmptyOrGroupIdIsNullOrEmpty_Then_Throw(string clientId, string groupId)
    {
        var action = async () => await _hubManager.AddToGroupAsync(clientId, groupId);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task When_ClientIsAddedToGroup_AndClientIsNotConnected_Then_ClientShouldNotBeAdded_And_GroupShouldNotBeCreatedIfNotExists()
    {
        const string clientId = "client1";
        const string clientId2 = "client2";
        const string groupId = "group1";
        const string groupId2 = "group2";

        await _hubManager.OnConnectedAsync(new WsHubContext(clientId2, Helpers.CreateFakeClientConnection(), null));

        await _hubManager.AddToGroupAsync(clientId, groupId);
        await _hubManager.AddToGroupAsync(clientId2, groupId2);
        await _hubManager.AddToGroupAsync(clientId, groupId2);

        _store.Groups.ContainsKey(groupId).Should().BeFalse();
        _store.Groups[groupId2].Should().NotContain(clientId).And.Contain(clientId2);
    }

    [Fact]
    public async Task When_ClientIsAddedToGroup_Then_ClientGroupsSetIsUpdated_And_GroupMembersSetIsUpdated()
    {
        const string clientId = "client1";
        const string clientId2 = "client2";

        var groupIds = new string[] { "group1", "group2" };
        const int group1MembersCount = 2;
        const int group2MembersCount = 1;

        const int client1GroupsCount = 1;
        const int client2GroupsCount = 2;

        await _hubManager.OnConnectedAsync(new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null));
        await _hubManager.OnConnectedAsync(new WsHubContext(clientId2, Helpers.CreateFakeClientConnection(), null));

        await _hubManager.AddToGroupAsync(clientId, groupIds[0]);
        await _hubManager.AddToGroupAsync(clientId2, groupIds);

        _store.Clients[clientId].GroupIds.Should().Contain(groupIds[0]).And.HaveCount(client1GroupsCount);
        _store.Clients[clientId2].GroupIds.Should().Contain(groupIds[0]).And.Contain(groupIds[1]).And.HaveCount(client2GroupsCount);

        _store.Groups[groupIds[0]].Should().HaveCount(group1MembersCount).And.Contain(clientId).And.Contain(clientId2);
        _store.Groups[groupIds[1]].Should().HaveCount(group2MembersCount).And.Contain(clientId2);
    }

    [Theory]
    [InlineData(null, "group1")]
    [InlineData("", "group1")]
    [InlineData("client1", null)]
    [InlineData("client1", "")]
    public async Task When_ClientIsRemovedFromGroup_And_ClientIdIsNullOrEmptyOrGroupIdIsNullOrEmpty_Then_Throw(string clientId, string groupId)
    {
        var action = async () => await _hubManager.RemoveFromGroupAsync(clientId, groupId);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task When_ClientIsRemovedFromGroup_Then_ClientGroupsSetIsUpdated_And_GroupMembersSetIsUpdatedAndGroupIsRemovedIfTheLastActiveClientWasRemoved()
    {
        const string clientId = "client1";
        const string clientId2 = "client2";
        var groupIds = new string[] { "group1", "group2" };

        const int client1GroupsCountAfterRemove = 1;
        const int group1MembersCountAfterRemove = 1;

        await _hubManager.OnConnectedAsync(new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null));
        await _hubManager.OnConnectedAsync(new WsHubContext(clientId2, Helpers.CreateFakeClientConnection(), null));

        await _hubManager.AddToGroupAsync(clientId, groupIds);
        await _hubManager.AddToGroupAsync(clientId2, groupIds[0]);

        await _hubManager.RemoveFromGroupAsync(clientId, groupIds[1]);
        await _hubManager.RemoveFromGroupAsync(clientId2, groupIds[0]);

        _store.Clients[clientId].GroupIds.Should().Contain(groupIds[0]).And.HaveCount(client1GroupsCountAfterRemove);
        _store.Clients[clientId2].GroupIds.Should().BeEmpty();

        _store.Groups.TryGetValue(groupIds[1], out _).Should().BeFalse();
        _store.Groups[groupIds[0]].Should().Contain(clientId).And.HaveCount(group1MembersCountAfterRemove);
    }

    [Fact]
    public async Task When_ClientConnects_And_ClientIsAlreadyConnected_Then_NewConnectionIsStored()
    {
        const string clientId = "client1";
        const int connectedClientsCount = 1;
        var context = new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null);
        var context2 = new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null);

        await _hubManager.OnConnectedAsync(context);
        await _hubManager.OnConnectedAsync(context2);

        _store.Clients.Should().HaveCount(connectedClientsCount);
        _store.Clients.TryGetValue(clientId, out WsClient? client).Should().BeTrue();
        client.Should().NotBeNull();
        client.Connections.Should().Contain(context.Connection).And.Contain(context2.Connection);
    }

    [Fact]
    public async Task When_ClientDisconnects_And_ContextIsNull_Then_Throw()
    {
        var action = async () => await _hubManager.OnDisconnectedAsync(null);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task When_ClientDisconnects_And_HasOnlyOneActiveConnection_Then_IsRemovedFromStoreAndAssociatedGroups()
    {
        const string clientId = "client1";
        const string clientId2 = "client2";
        var groupId = "group1";
        var groupId2 = "group2";
        var context = new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null);
        var context2 = new WsHubContext(clientId2, Helpers.CreateFakeClientConnection(), null);

        await _hubManager.OnConnectedAsync(context);
        await _hubManager.OnConnectedAsync(context2);

        await _hubManager.AddToGroupAsync(clientId, groupId);
        await _hubManager.AddToGroupAsync(clientId2, groupId);
        await _hubManager.AddToGroupAsync(clientId, groupId2);

        await _hubManager.OnDisconnectedAsync(context);
        await _hubManager.OnDisconnectedAsync(context2);

        // Expected to be empty because client should be removed if last active connection is removed
        _store.Clients.Should().BeEmpty();

        // Expected to be false because the groups should be removed if last active member is removed
        _store.Groups.ContainsKey(groupId).Should().BeFalse();
        _store.Groups.ContainsKey(groupId2).Should().BeFalse();
    }

    [Fact]
    public async Task When_ClientDisconnects_And_HasMoreThanOneActiveConnections_Then_ConnectionIsRemovedFromStore_AndClient_IsNotRemoved_FromGroups()
    {
        const string clientId = "client1";
        var groupId = "group1";
        var groupId2 = "group2";

        var context = new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null);
        var context2 = new WsHubContext(clientId, Helpers.CreateFakeClientConnection(), null);

        await _hubManager.OnConnectedAsync(context);
        await _hubManager.OnConnectedAsync(context2);

        await _hubManager.AddToGroupAsync(clientId, groupId);
        await _hubManager.AddToGroupAsync(clientId, groupId2);

        await _hubManager.OnDisconnectedAsync(context2);

        const int remainingConnectionsCount = 1;

        _store.Clients.TryGetValue(clientId, out WsClient? client).Should().BeTrue();
        client.Should().NotBeNull();
        client.Connections.Should().HaveCount(remainingConnectionsCount).And.AllBeEquivalentTo(context.Connection);
        client.GroupIds.Should().Contain(groupId).And.Contain(groupId2);

        _store.Groups[groupId].Should().Contain(clientId);
        _store.Groups[groupId2].Should().Contain(clientId);
    }
}
