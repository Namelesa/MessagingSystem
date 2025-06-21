using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.WebApi;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages.Contracts;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Infrastructure;

public class GroupChatTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IGroupInfoOrchestrator> _groupInfoOrchestrator = new();
    private readonly Mock<IGroupMessagesOrchestrator> _groupMessagesOrchestrator = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IHasher> _hasher = new();

    private const string SecretKey = "your-super-secure-test-key-must-be-32chars";
    private const string HubUrl = "http://localhost:5040/groupChatHub";

    public GroupChatTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("urls", "http://localhost:5040");
            builder.ConfigureServices(services =>
            {
                services.AddSignalR().AddNewtonsoftJsonProtocol();

                services.RemoveAll<IGroupInfoOrchestrator>();
                services.RemoveAll<IGroupMessagesOrchestrator>();
                services.RemoveAll<IMapper>();
                services.RemoveAll<IHasher>();
                services.RemoveAll<IPolicyEvaluator>();

                services.AddSingleton(_groupInfoOrchestrator.Object);
                services.AddSingleton(_groupMessagesOrchestrator.Object);
                services.AddSingleton(_mapper.Object);
                services.AddSingleton(_hasher.Object);
                services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();
            });
        });
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendMessageAndNotifyGroup()
    {
        var groupId = Guid.NewGuid();
        var content = "Test message";
        var nickname = "testuser";
        var messageId = Guid.NewGuid();

        var messageDto = new GroupMessageDto(nickname, content, groupId);
        var result = new CreatedMessageResult(messageId, DateTime.UtcNow);

        _mapper.Setup(m => m.Map<GroupMessageDto>(It.IsAny<CreateGroupMessage>()))
            .Returns(messageDto);

        _groupMessagesOrchestrator.Setup(m => m.SendMessageAsync(messageDto))
            .ReturnsAsync(OperationResult<CreatedMessageResult>.Ok(result));

        var connection = await CreateConnectionAsync(nickname);

        var receivedMessages = new List<GroupMessageDto>();
        connection.On<GroupMessageDto>("ReceiveMessage", msg => receivedMessages.Add(msg));

        await connection.InvokeAsync("JoinGroupAsync", groupId);

        await connection.InvokeAsync<CreatedMessageResult>("SendMessageAsync", content, groupId);
        await Task.Delay(100);

        Assert.Single(receivedMessages);


        Assert.Single(receivedMessages);
        Assert.Equal(nickname, receivedMessages[0].Sender);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task AddMembersToGroupAsync_ShouldAddAndNotify()
    {
        var groupId = Guid.NewGuid();
        var nickname = "testuser";
        var members = new AddMembers { Users = ["user1", "user2"] };
        var groupDto = new GroupDto("group", "img", "desc", nickname, ["user1"], [1]);

        _mapper.Setup(x => x.Map<GroupMembersDto>(members))
            .Returns(new GroupMembersDto());
        _hasher.Setup(x => x.Hash(nickname)).Returns("hashed");

        _groupInfoOrchestrator.Setup(x =>
                x.AddMembersToGroupAsync(groupId, It.IsAny<GroupMembersDto>(), "hashed"))
            .ReturnsAsync(OperationResult<GroupDto>.Ok(groupDto));

        var connection = await CreateConnectionAsync(nickname);
        var notified = new List<GroupDto>();
        connection.On<GroupDto>("GroupMembersAdded", dto => notified.Add(dto));

        await connection.InvokeAsync("JoinGroupAsync", groupId);
        
        await connection.InvokeAsync<GroupDto>("AddMembersToGroupAsync", groupId, members);
        await Task.Delay(100);

        Assert.Single(notified);
        Assert.Equal("group", notified[0].GroupName);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task EditMessageAsync_ShouldNotify()
    {
        var nickname = "testuser";
        var messageId = Guid.NewGuid();
        var groupId = Guid.NewGuid().ToString();
        var newContent = "Edited content";

        _groupMessagesOrchestrator.Setup(x => x.EditMessageAsync(messageId, It.IsAny<EditMessageDto>()))
            .ReturnsAsync(OperationResult<string>.Ok(groupId));

        var connection = await CreateConnectionAsync(nickname);
        var notifications = new List<object>();
        connection.On<object>("MessageEdited", o => notifications.Add(o));

        await connection.InvokeAsync("JoinGroupAsync", groupId);
        
        await connection.InvokeAsync("EditMessageAsync", messageId, newContent);
        await Task.Delay(100);

        Assert.Single(notifications);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetAllGroupForUserAsync_ShouldReturnUserGroups()
    {
        // Arrange
        var nickname = "testuser";
        var expectedGroups = new List<GroupDto>
        {
            new("Group A", "img", "desc", nickname, ["user1"], [1]),
            new("Group B", "img", "desc", nickname, ["user2"], [2])
        };

        _groupInfoOrchestrator
            .Setup(x => x.GetGroupsForUserAsync(nickname))
            .ReturnsAsync(OperationResult<List<GroupDto>>.Ok(expectedGroups));

        var connection = await CreateConnectionAsync(nickname);

        // Act
        var result = await connection.InvokeAsync<List<GroupDto>>("GetAllGroupForUserAsync");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Group A", result[0].GroupName);
        Assert.Equal("Group B", result[1].GroupName);

        await connection.DisposeAsync();
    }
    
    [Fact]
    public async Task CreateGroupAsync_ShouldCreateAndReturnGroup()
    {
        // Arrange
        var nickname = "testuser";
        var createGroup = new CreateGroup("Test Group", "img", "Test Description", "admin");
        var expectedGroup = new GroupDto("Test Group", "img", "Test Description", nickname, [nickname], [1]);

        _mapper.Setup(m => m.Map<GroupDto>(It.IsAny<CreateGroup>()))
            .Returns(expectedGroup);

        _groupInfoOrchestrator.Setup(x => x.CreateGroupAsync(It.IsAny<GroupDto>()))
            .ReturnsAsync(OperationResult<GroupDto>.Ok(expectedGroup));

        var connection = await CreateConnectionAsync(nickname);

        // Act
        var result = await connection.InvokeAsync<GroupDto>("CreateGroupAsync", createGroup);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Group", result.GroupName);
        Assert.Equal(nickname, result.Admin);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task EditGroupAsync_ShouldEditAndReturnGroup()
    {
        // Arrange
        var nickname = "testuser";
        var groupId = Guid.NewGuid();
        var editGroup = new EditGroup("Updated Group", "image", "description");
        var expectedGroup = new GroupDto("Updated Group", "img", "Updated Description", nickname, [nickname], [1]);

        _mapper.Setup(m => m.Map<EditGroupDto>(It.IsAny<EditGroup>()))
            .Returns(new EditGroupDto("Updated Group", "image", "description"));

        _groupInfoOrchestrator.Setup(x => x.EditGroupInfoAsync(groupId, It.IsAny<EditGroupDto>()))
            .ReturnsAsync(OperationResult<GroupDto>.Ok(expectedGroup));

        var connection = await CreateConnectionAsync(nickname);

        // Act
        var result = await connection.InvokeAsync<GroupDto>("EditGroupAsync", groupId, editGroup);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Group", result.GroupName);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldDeleteAndReturnGroupId()
    {
        // Arrange
        var nickname = "testuser";
        var groupId = Guid.NewGuid();
        var expectedResult = groupId.ToString();

        _groupInfoOrchestrator.Setup(x => x.DeleteGroupInfoAsync(groupId, nickname))
            .ReturnsAsync(OperationResult<string>.Ok(expectedResult));

        var connection = await CreateConnectionAsync(nickname);

        // Act
        var result = await connection.InvokeAsync<string>("DeleteGroupAsync", groupId);

        // Assert
        Assert.Equal(expectedResult, result);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task RemoveMembersFromGroupAsync_ShouldRemoveAndNotify()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var nickname = "testuser";
        var members = new AddMembers { Users = ["user1", "user2"] };
        var groupDto = new GroupDto("group", "img", "desc", nickname, ["user3"], [1]);

        _mapper.Setup(x => x.Map<GroupMembersDto>(members))
            .Returns(new GroupMembersDto());
        _hasher.Setup(x => x.Hash(nickname)).Returns("hashed");

        _groupInfoOrchestrator.Setup(x =>
                x.DeleteMembersFromGroupAsync(groupId, It.IsAny<GroupMembersDto>(), "hashed"))
            .ReturnsAsync(OperationResult<GroupDto>.Ok(groupDto));

        var connection = await CreateConnectionAsync(nickname);
        var notified = new List<GroupDto>();
        connection.On<GroupDto>("GroupMembersRemoved", dto => notified.Add(dto));

        await connection.InvokeAsync("JoinGroupAsync", groupId);

        // Act
        await connection.InvokeAsync<GroupDto>("RemoveMembersFromGroupAsync", groupId, members);
        await Task.Delay(100);

        // Assert
        Assert.Single(notified);
        Assert.Equal("group", notified[0].GroupName);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task LoadChatHistoryAsync_ShouldReturnMessages()
{
    // Arrange
    var nickname = "testuser";
    var groupId = Guid.NewGuid();
    var take = 10;
    var expectedMessages = new List<GroupMessage>
    {
        new(nickname, "Message 1"),
        new(nickname, "Message 2")
    };

    _groupMessagesOrchestrator.Setup(x => x.LoadChatHistory(groupId, take))
        .ReturnsAsync(expectedMessages);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<GroupMessage>>("LoadChatHistoryAsync", groupId, take);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Equal("Message 1", result[0].Content);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task SofDeleteMessageAsync_ShouldNotify()
{
    // Arrange
    var nickname = "testuser";
    var messageId = Guid.NewGuid();
    var groupId = Guid.NewGuid().ToString();

    _groupMessagesOrchestrator.Setup(x => x.SoftDeleteMessageAsync(messageId))
        .ReturnsAsync(OperationResult<string>.Ok(groupId));

    var connection = await CreateConnectionAsync(nickname);
    var notifications = new List<Guid>();
    connection.On<Guid>("MessageSoftDeleted", id => notifications.Add(id));

    await connection.InvokeAsync("JoinGroupAsync", groupId);

    // Act
    await connection.InvokeAsync("SofDeleteMessageAsync", messageId);
    await Task.Delay(100);

    // Assert
    Assert.Single(notifications);
    Assert.Equal(messageId, notifications[0]);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task DeleteMessageAsync_ShouldNotify()
{
    // Arrange
    var nickname = "testuser";
    var messageId = Guid.NewGuid();
    var groupId = Guid.NewGuid().ToString();

    _groupMessagesOrchestrator.Setup(x => x.DeleteMessageAsync(messageId))
        .ReturnsAsync(OperationResult<string>.Ok(groupId));

    var connection = await CreateConnectionAsync(nickname);
    var notifications = new List<Guid>();
    connection.On<Guid>("MessageDeleted", id => notifications.Add(id));

    await connection.InvokeAsync("JoinGroupAsync", groupId);

    // Act
    await connection.InvokeAsync("DeleteMessageAsync", messageId);
    await Task.Delay(100);

    // Assert
    Assert.Single(notifications);
    Assert.Equal(messageId, notifications[0]);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task ReplyForMessageAsync_ShouldCreateReplyAndNotify()
{
    // Arrange
    var nickname = "testuser";
    var messageId = Guid.NewGuid();
    var replyToMessageId = Guid.NewGuid();
    var groupId = Guid.NewGuid();
    var content = "Reply message";
    var sentTime = DateTime.UtcNow;

    var sendResult = new CreatedMessageResult(messageId, sentTime);
    var replyMessage = new GroupMessage(nickname, content);

    _groupMessagesOrchestrator.Setup(x => x.SendMessageAsync(It.IsAny<GroupMessageDto>()))
        .ReturnsAsync(OperationResult<CreatedMessageResult>.Ok(sendResult));

    _groupMessagesOrchestrator.Setup(x => x.ReplyForMessageAsync(messageId, replyToMessageId))
        .ReturnsAsync(OperationResult<GroupMessage>.Ok(replyMessage));

    var connection = await CreateConnectionAsync(nickname);
    var notifications = new List<object>();
    connection.On<object>("MessageReplied", obj => notifications.Add(obj));

    await connection.InvokeAsync("JoinGroupAsync", groupId);

    // Act
    var result = await connection.InvokeAsync<object>("ReplyForMessageAsync", replyToMessageId, content, groupId);
    await Task.Delay(100);

    // Assert
    Assert.NotNull(result);
    Assert.Single(notifications);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessageByFilterAsync_ShouldReturnFilteredMessages()
{
    // Arrange
    var nickname = "testuser";
    var sender = "user1";
    var recipient = "user2";
    var time = DateTime.UtcNow;

    var messages = new List<GroupMessage>
    {
        new(sender, "Test message")
    };

    _groupMessagesOrchestrator.Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .ReturnsAsync(messages);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessageByFilterAsync", recipient, time, sender);

    // Assert
    Assert.NotNull(result);
    Assert.Single(result);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task JoinGroupAsync_ShouldAddConnectionToGroup()
{
    // Arrange
    var nickname = "testuser";
    var groupId = Guid.NewGuid();

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await connection.InvokeAsync("JoinGroupAsync", groupId);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task LeaveGroupAsync_ShouldRemoveConnectionFromGroup()
{
    // Arrange
    var nickname = "testuser";
    var groupId = Guid.NewGuid();

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await connection.InvokeAsync("LeaveGroupAsync", groupId);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task CreateGroupAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var nickname = "testuser";
    var createGroup = new CreateGroup("Test Group", "image", "description", "admin");

    _mapper.Setup(m => m.Map<GroupDto>(It.IsAny<CreateGroup>()))
        .Returns(new GroupDto("Test Group", "", "", nickname, [], []));

    _groupInfoOrchestrator.Setup(x => x.CreateGroupAsync(It.IsAny<GroupDto>()))
        .ReturnsAsync(OperationResult<GroupDto>.Fail("Creation failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<GroupDto>("CreateGroupAsync", createGroup));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task EditGroupAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var nickname = "testuser";
    var groupId = Guid.NewGuid();
    var editGroup = new EditGroup("Updated Group", "image", "description");

    _mapper.Setup(m => m.Map<EditGroupDto>(It.IsAny<EditGroup>()))
        .Returns(new EditGroupDto("Updated Group", "image", "description"));

    _groupInfoOrchestrator.Setup(x => x.EditGroupInfoAsync(groupId, It.IsAny<EditGroupDto>()))
        .ReturnsAsync(OperationResult<GroupDto>.Fail("Edit failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<GroupDto>("EditGroupAsync", groupId, editGroup));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task DeleteGroupAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var nickname = "testuser";
    var groupId = Guid.NewGuid();

    _groupInfoOrchestrator.Setup(x => x.DeleteGroupInfoAsync(groupId, nickname))
        .ReturnsAsync(OperationResult<string>.Fail("Delete failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<string>("DeleteGroupAsync", groupId));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task AddMembersToGroupAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var groupId = Guid.NewGuid();
    var nickname = "testuser";
    var members = new AddMembers { Users = ["user1"] };

    _mapper.Setup(x => x.Map<GroupMembersDto>(members)).Returns(new GroupMembersDto());
    _hasher.Setup(x => x.Hash(nickname)).Returns("hashed");

    _groupInfoOrchestrator.Setup(x => 
            x.AddMembersToGroupAsync(groupId, It.IsAny<GroupMembersDto>(), "hashed"))
        .ReturnsAsync(OperationResult<GroupDto>.Fail("Add members failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<GroupDto>("AddMembersToGroupAsync", groupId, members));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task RemoveMembersFromGroupAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var groupId = Guid.NewGuid();
    var nickname = "testuser";
    var members = new AddMembers { Users = ["user1"] };

    _mapper.Setup(x => x.Map<GroupMembersDto>(members)).Returns(new GroupMembersDto());
    _hasher.Setup(x => x.Hash(nickname)).Returns("hashed");

    _groupInfoOrchestrator.Setup(x =>
            x.DeleteMembersFromGroupAsync(groupId, It.IsAny<GroupMembersDto>(), "hashed"))
        .ReturnsAsync(OperationResult<GroupDto>.Fail("Remove members failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<GroupDto>("RemoveMembersFromGroupAsync", groupId, members));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task SendMessageAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var groupId = Guid.NewGuid();
    var content = "Test message";
    var nickname = "testuser";

    var messageDto = new GroupMessageDto(nickname, content, groupId);

    _mapper.Setup(m => m.Map<GroupMessageDto>(It.IsAny<CreateGroupMessage>()))
        .Returns(messageDto);

    _groupMessagesOrchestrator.Setup(m => m.SendMessageAsync(messageDto))
        .ReturnsAsync(OperationResult<CreatedMessageResult>.Fail("Send failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<CreatedMessageResult>("SendMessageAsync", content, groupId));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task EditMessageAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var nickname = "testuser";
    var messageId = Guid.NewGuid();
    var newContent = "Edited content";

    _groupMessagesOrchestrator.Setup(x => x.EditMessageAsync(messageId, It.IsAny<EditMessageDto>()))
        .ReturnsAsync(OperationResult<string>.Fail("Edit message failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync("EditMessageAsync", messageId, newContent));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task SofDeleteMessageAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var nickname = "testuser";
    var messageId = Guid.NewGuid();

    _groupMessagesOrchestrator.Setup(x => x.SoftDeleteMessageAsync(messageId))
        .ReturnsAsync(OperationResult<string>.Fail("Soft delete failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync("SofDeleteMessageAsync", messageId));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task DeleteMessageAsync_ShouldThrowHubException_WhenOperationFails()
{
    // Arrange
    var nickname = "testuser";
    var messageId = Guid.NewGuid();

    _groupMessagesOrchestrator.Setup(x => x.DeleteMessageAsync(messageId))
        .ReturnsAsync(OperationResult<string>.Fail("Delete failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync("DeleteMessageAsync", messageId));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task ReplyForMessageAsync_ShouldThrowHubException_WhenSendMessageFails()
{
    // Arrange
    var nickname = "testuser";
    var replyToMessageId = Guid.NewGuid();
    var groupId = Guid.NewGuid();
    var content = "Reply message";

    _groupMessagesOrchestrator.Setup(x => x.SendMessageAsync(It.IsAny<GroupMessageDto>()))
        .ReturnsAsync(OperationResult<CreatedMessageResult>.Fail("Send failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<object>("ReplyForMessageAsync", replyToMessageId, content, groupId));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task ReplyForMessageAsync_ShouldThrowHubException_WhenReplyAttachmentFails()
{
    // Arrange
    var nickname = "testuser";
    var messageId = Guid.NewGuid();
    var replyToMessageId = Guid.NewGuid();
    var groupId = Guid.NewGuid();
    var content = "Reply message";
    var sentTime = DateTime.UtcNow;

    var sendResult = new CreatedMessageResult(messageId, sentTime);

    _groupMessagesOrchestrator.Setup(x => x.SendMessageAsync(It.IsAny<GroupMessageDto>()))
        .ReturnsAsync(OperationResult<CreatedMessageResult>.Ok(sendResult));

    _groupMessagesOrchestrator.Setup(x => x.ReplyForMessageAsync(messageId, replyToMessageId))
        .ReturnsAsync(OperationResult<GroupMessage>.Fail("Reply attachment failed"));

    var connection = await CreateConnectionAsync(nickname);

    // Act & Assert
    await Assert.ThrowsAsync<HubException>(() => 
        connection.InvokeAsync<object>("ReplyForMessageAsync", replyToMessageId, content, groupId));

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessageByFilterAsync_ShouldReturnEmptyList_WhenNoMessagesFound()
{
    // Arrange
    var nickname = "testuser";
    var sender = "user1";
    var recipient = "user2";
    var time = DateTime.UtcNow;

    _groupMessagesOrchestrator.Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .ReturnsAsync((List<GroupMessage>)null);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessageByFilterAsync", recipient, time, sender);

    // Assert
    Assert.NotNull(result);
    Assert.Empty(result);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessageByFilterAsync_ShouldReturnEmptyList_WhenMessagesListIsEmpty()
{
    // Arrange
    var nickname = "testuser";
    var sender = "user1";
    var recipient = "user2";
    var time = DateTime.UtcNow;

    _groupMessagesOrchestrator.Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .ReturnsAsync(new List<GroupMessage>());

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessageByFilterAsync", recipient, time, sender);

    // Assert
    Assert.NotNull(result);
    Assert.Empty(result);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessageByFilterAsync_ShouldHandleNullParameters()
{
    // Arrange
    var nickname = "testuser";
    var messages = new List<GroupMessage>
    {
        new("user1", "Test message") 
    };

    _groupMessagesOrchestrator.Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .ReturnsAsync(messages);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessageByFilterAsync", null, null, null);

    // Assert
    Assert.NotNull(result);
    Assert.Single(result);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessageByFilterAsync_ShouldHandleMultipleMessages()
{
    // Arrange
    var nickname = "testuser";
    var sender = "user1";
    var recipient = "user2";
    var time = DateTime.UtcNow;

    var messages = new List<GroupMessage>
    {
        new(sender, "First message"),
        new(sender, "Second message") 
    };

    _groupMessagesOrchestrator.Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .ReturnsAsync(messages);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessageByFilterAsync", recipient, time, sender);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessageByFilterAsync_ShouldSetCorrectFilterProperties()
{
    // Arrange
    var nickname = "testuser";
    var sender = "user1";
    var recipient = "user2";
    var time = DateTime.UtcNow;

    MessageFilter capturedFilter = null;
    _groupMessagesOrchestrator.Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .Callback<MessageFilter>(f => capturedFilter = f)
        .ReturnsAsync(new List<GroupMessage>());

    var connection = await CreateConnectionAsync(nickname);

    // Act
    await connection.InvokeAsync<List<object>>("FindMessageByFilterAsync", recipient, time, sender);

    // Assert
    Assert.NotNull(capturedFilter);
    Assert.Equal(sender, capturedFilter.Sender);
    Assert.Equal(recipient, capturedFilter.Recipient);
    Assert.Equal(time, capturedFilter.Date);

    await connection.DisposeAsync();
}

    private async Task<HubConnection> CreateConnectionAsync(string nickname)
    {
        var token = GenerateJwtToken(nickname);

        var connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .AddNewtonsoftJsonProtocol()
            .Build();

        await connection.StartAsync();
        return connection;
    }
    
    private static string GenerateJwtToken(string nickname)
    {
        var key = Encoding.UTF8.GetBytes(SecretKey);
        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.UserData, nickname) }),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}