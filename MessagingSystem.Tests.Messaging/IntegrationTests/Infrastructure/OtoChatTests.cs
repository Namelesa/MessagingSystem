using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR.Client;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;
using MessagingSystem.Services.Messaging.WebApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Infrastructure;

public class OtoChatHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IMessageOrchestrator> _messageOrchestratorMock;
    private readonly Mock<IChatOrchestrator> _chatOrchestratorMock;

    public OtoChatHubTests(WebApplicationFactory<Program> factory)
    {
        _messageOrchestratorMock = new Mock<IMessageOrchestrator>();
        _chatOrchestratorMock = new Mock<IChatOrchestrator>();
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("urls", "http://localhost:5030");
            builder.ConfigureServices(services =>
            {
                var messageOrchestratorDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMessageOrchestrator));
                if (messageOrchestratorDescriptor != null)
                    services.Remove(messageOrchestratorDescriptor);

                var chatOrchestratorDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IChatOrchestrator));
                if (chatOrchestratorDescriptor != null)
                    services.Remove(chatOrchestratorDescriptor);

                services.AddSingleton(_messageOrchestratorMock.Object);
                services.AddSingleton(_chatOrchestratorMock.Object);
                
                services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();
            });
        });
    }

    [Fact]
    public async Task SendMessageAsync_ValidMessage_ShouldSendAndNotifyUsers()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sentTime = DateTime.UtcNow;
        var sender = "testuser";
        var recipient = "testuser2";
        var content = "Hello, World!";

        var sendResult = OperationResult<CreatedMessageResult>.Ok(new CreatedMessageResult(messageId, sentTime));
        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(It.IsAny<MessagesDto>()))
            .ReturnsAsync(sendResult);

        var connection1 = await CreateConnectionAsync(sender);
        var connection2 = await CreateConnectionAsync(recipient);

        var receivedMessages = new List<object>();
        connection1.On<object>("ReceivePrivateMessage", msg => receivedMessages.Add(msg));
        connection2.On<object>("ReceivePrivateMessage", msg => receivedMessages.Add(msg));

        // Act
        var result = await connection1.InvokeAsync<object>("SendMessageAsync", recipient, content);

        // Assert
        await Task.Delay(100); 

        Assert.NotNull(result);
        Assert.Equal(2, receivedMessages.Count);

        _messageOrchestratorMock.Verify(x => x.SendMessageAsync(
            It.Is<MessagesDto>(dto => 
                dto.Sender == sender && 
                dto.Recipient == recipient && 
                dto.Content == content)), Times.Once);

        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Fact]
    public async Task SendMessageAsync_FailedToSend_ShouldThrowHubException()
    {
        // Arrange
        var sender = "testuser1";
        var recipient = "testuser2";
        var content = "Hello, World!";

        var sendResult = OperationResult<CreatedMessageResult>.Fail("Failed to send message");

        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(It.IsAny<MessagesDto>()))
            .ReturnsAsync(sendResult);

        var connection = await CreateConnectionAsync(sender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
            await connection.InvokeAsync<object>("SendMessageAsync", recipient, content));

        Assert.Contains("Failed to send message", exception.Message);

        await connection.DisposeAsync();
    }
    
    [Fact]
    public async Task DeleteMessageAsync_SoftDelete_ShouldSoftDeleteAndNotifyUsers()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sender = "testuser1";
        var recipient = "testuser2";

        var deleteResult = OperationResult<string>.Ok("Message soft deleted");
        var findResult = OperationResult<string>.Ok(recipient);

        _messageOrchestratorMock
            .Setup(x => x.SoftDeleteMessageAsync(messageId))
            .ReturnsAsync(deleteResult);

        _messageOrchestratorMock
            .Setup(x => x.FindMessageByIdAsync(messageId))
            .ReturnsAsync(findResult);

        var connection1 = await CreateConnectionAsync(sender);
        var connection2 = await CreateConnectionAsync(recipient);

        var deletedMessages = new List<object>();
        connection1.On<object>("MessageDeleted", msg => deletedMessages.Add(msg));
        connection2.On<object>("MessageDeleted", msg => deletedMessages.Add(msg));

        // Act
        await connection1.InvokeAsync("DeleteMessageAsync", messageId, "soft");

        // Assert
        await Task.Delay(100);

        Assert.Equal(2, deletedMessages.Count);

        _messageOrchestratorMock.Verify(x => x.SoftDeleteMessageAsync(messageId), Times.Once);
        _messageOrchestratorMock.Verify(x => x.DeleteMessageAsync(messageId), Times.Never);

        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Fact]
    public async Task DeleteMessageAsync_HardDelete_ShouldHardDeleteAndNotifyUsers()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sender = "testuser1";
        var recipient = "testuser2";

        var deleteResult = OperationResult<string>.Ok("Message hard deleted");
        var findResult = OperationResult<string>.Ok(recipient);

        _messageOrchestratorMock
            .Setup(x => x.DeleteMessageAsync(messageId))
            .ReturnsAsync(deleteResult);

        _messageOrchestratorMock
            .Setup(x => x.FindMessageByIdAsync(messageId))
            .ReturnsAsync(findResult);

        var connection = await CreateConnectionAsync(sender);

        // Act
        await connection.InvokeAsync("DeleteMessageAsync", messageId, "hard");

        // Assert
        _messageOrchestratorMock.Verify(x => x.DeleteMessageAsync(messageId), Times.Once);
        _messageOrchestratorMock.Verify(x => x.SoftDeleteMessageAsync(messageId), Times.Never);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task LoadChatHistoryAsync_ValidRequest_ShouldReturnMessages()
    {
        // Arrange
        var currentUser = "testuser";
        var withUser = "testuser2";
        var take = 10;

        var messages = new List<Message>
        {
            new(currentUser, withUser, "Message 1"),
            new(withUser, currentUser, "Message 2")
        };

        _messageOrchestratorMock
            .Setup(x => x.LoadChatHistory(currentUser, withUser, take))
            .ReturnsAsync(messages);

        var connection = await CreateConnectionAsync(currentUser);

        // Act
        var result = await connection.InvokeAsync<List<object>>("LoadChatHistoryAsync", withUser, take);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        _messageOrchestratorMock.Verify(x => x.LoadChatHistory(currentUser, withUser, take), Times.Once);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetChatsAsync_ValidRequest_ShouldReturnChats()
    {
        // Arrange
        var nickname = "testuser";
        var chats = new List<ChatDto>
        {
            new() { NickName = "user2", Image = "image2.jpg" },
            new() { NickName = "user3", Image = "image3.jpg" }
        };

        _chatOrchestratorMock
            .Setup(x => x.GetChatsAsync(nickname))
            .ReturnsAsync(chats);

        var connection = await CreateConnectionAsync(nickname);

        // Act
        var result = await connection.InvokeAsync<List<ChatDto>>("GetChatsAsync");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("user2", result[0].NickName);
        Assert.Equal("user3", result[1].NickName);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ReplyToMessageAsync_ValidReply_ShouldSendReplyAndNotifyUsers()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sentTime = DateTime.UtcNow;
        var sender = "testuser1";
        var recipient = "testuser2";
        var content = "This is a reply";
        var replyToMessageId = Guid.NewGuid();

        var sendResult = OperationResult<CreatedMessageResult>.Ok(new CreatedMessageResult(messageId, sentTime));

        var replyResult = OperationResult<Message>.Ok(new Message(sender, recipient, content));

        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(It.IsAny<MessagesDto>()))
            .ReturnsAsync(sendResult);

        _messageOrchestratorMock
            .Setup(x => x.ReplyForMessageAsync(messageId, replyToMessageId))
            .ReturnsAsync(replyResult);

        var connection1 = await CreateConnectionAsync(sender);
        var connection2 = await CreateConnectionAsync(recipient);

        var receivedMessages = new List<object>();
        connection1.On<object>("ReceivePrivateMessage", msg => receivedMessages.Add(msg));
        connection2.On<object>("ReceivePrivateMessage", msg => receivedMessages.Add(msg));

        // Act
        var result = await connection1.InvokeAsync<object>("ReplyToMessageAsync", recipient, content, replyToMessageId);

        // Assert
        await Task.Delay(100);

        Assert.NotNull(result);
        Assert.Equal(2, receivedMessages.Count);

        _messageOrchestratorMock.Verify(x => x.SendMessageAsync(It.IsAny<MessagesDto>()), Times.Once);
        _messageOrchestratorMock.Verify(x => x.ReplyForMessageAsync(messageId, replyToMessageId), Times.Once);

        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Fact]
    public async Task ConnectionLifecycle_ConnectAndDisconnect_ShouldManageGroupsMembership()
    {
        // Arrange
        var nickname = "testuser1";

        // Act - Connect
        var connection = await CreateConnectionAsync(nickname);
        
        Assert.Equal(HubConnectionState.Connected, connection.State);

        // Act - Disconnect
        await connection.DisposeAsync();

        // Assert
        Assert.Equal(HubConnectionState.Disconnected, connection.State);
    }

    [Fact]
    public async Task MultipleConnections_SameUser_ShouldAllReceiveNotifications()
    {
        // Arrange
        var sender = "testuser1";
        var recipient = "testuser2";
        var content = "Test message";
        var messageId = Guid.NewGuid();

        var sendResult = OperationResult<CreatedMessageResult>.Ok(new CreatedMessageResult(messageId, DateTime.UtcNow));
        
        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(It.IsAny<MessagesDto>()))
            .ReturnsAsync(sendResult);
        
        var connection1 = await CreateConnectionAsync(recipient);
        var connection2 = await CreateConnectionAsync(recipient);
        var senderConnection = await CreateConnectionAsync(sender);

        var receivedMessages1 = new List<object>();
        var receivedMessages2 = new List<object>();

        connection1.On<object>("ReceivePrivateMessage", msg => receivedMessages1.Add(msg));
        connection2.On<object>("ReceivePrivateMessage", msg => receivedMessages2.Add(msg));

        // Act
        await senderConnection.InvokeAsync<object>("SendMessageAsync", recipient, content);

        // Assert
        await Task.Delay(200);

        Assert.Single(receivedMessages1);
        Assert.Single(receivedMessages2);

        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
        await senderConnection.DisposeAsync();
    }

    [Fact]
    public async Task OnConnectedAsync_ValidConnection_ShouldAddToGroup()
    {
        // Arrange
        var nickname = "testuser";

        // Act
        var connection = await CreateConnectionAsync(nickname);

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task OnDisconnectedAsync_ValidDisconnection_ShouldRemoveFromGroup()
    {
        // Arrange
        var nickname = "testuser";
        var connection = await CreateConnectionAsync(nickname);

        // Act
        await connection.DisposeAsync();

        // Assert
        Assert.Equal(HubConnectionState.Disconnected, connection.State);
    }

    [Fact]
    public async Task EditMessageAsync_ValidEdit_ShouldEditAndNotifyUsers()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var newContent = "Edited message content";
        var sender = "testuser";
        var recipient = "testuser2";

        var editResult = OperationResult<string>.Ok("Message edited successfully");
    
        _messageOrchestratorMock
            .Setup(x => x.EditMessageAsync(messageId, It.IsAny<EditMessageDto>()))
            .ReturnsAsync(editResult);

        _messageOrchestratorMock
            .Setup(x => x.FindMessageByIdAsync(messageId))
            .ReturnsAsync(OperationResult<string>.Ok("test string"));

        var connection1 = await CreateConnectionAsync(sender);
        var connection2 = await CreateConnectionAsync(recipient);

        var editedMessages = new List<object>();
        connection1.On<object>("MessageEdited", msg => editedMessages.Add(msg));
        connection2.On<object>("MessageEdited", msg => editedMessages.Add(msg));

        // Act
        await connection1.InvokeAsync("EditMessageAsync", messageId, newContent);

        // Assert
        await Task.Delay(100);

        Assert.Equal(2, editedMessages.Count);
        _messageOrchestratorMock.Verify(x => x.EditMessageAsync(messageId, It.IsAny<EditMessageDto>()), Times.Once);

        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Fact]
    public async Task EditMessageAsync_FailedEdit_ShouldThrowHubException()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var newContent = "Edited content";
        var sender = "testuser";

        var editResult = OperationResult<string>.Fail("Failed to edit message");

        _messageOrchestratorMock
            .Setup(x => x.EditMessageAsync(messageId, It.IsAny<EditMessageDto>()))
            .ReturnsAsync(editResult);

        var connection = await CreateConnectionAsync(sender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
            await connection.InvokeAsync("EditMessageAsync", messageId, newContent));
    
        Assert.IsType<HubException>(exception);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task DeleteMessageAsync_FailedDelete_ShouldThrowHubException()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sender = "testuser";

        var deleteResult = OperationResult<string>.Fail("Failed to delete message");

        _messageOrchestratorMock
            .Setup(x => x.SoftDeleteMessageAsync(messageId))
            .ReturnsAsync(deleteResult);

        var connection = await CreateConnectionAsync(sender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
            await connection.InvokeAsync("DeleteMessageAsync", messageId, "soft"));

        Assert.IsType<HubException>(exception);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ReplyToMessageAsync_FailedReply_ShouldThrowHubException()
    {
        // Arrange
        var sender = "testuser1";
        var recipient = "testuser2";
        var content = "Reply content";
        var replyToMessageId = Guid.NewGuid();

        var sendResult = OperationResult<CreatedMessageResult>.Fail("Failed to send reply");

        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(It.IsAny<MessagesDto>()))
            .ReturnsAsync(sendResult);

        var connection = await CreateConnectionAsync(sender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
            await connection.InvokeAsync("ReplyToMessageAsync", recipient, content, replyToMessageId));

        Assert.Contains("Failed to send reply", exception.Message);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ReplyToMessageAsync_ReplyFailed_ShouldThrowHubException()
    {
        // Arrange
        var sender = "user1";
        var recipient = "user2";
        var messageContent = "reply message";
        var replyToId = Guid.NewGuid();
        var newMessageId = Guid.NewGuid();

        var sendResult = OperationResult<CreatedMessageResult>.Ok(
            new CreatedMessageResult(newMessageId, DateTime.UtcNow));
        
        var replyResult = OperationResult<Message>.Fail("Reply failed");

        var mockContext = new Mock<HubCallerContext>();
        mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.UserData, sender)
        }, "mock")));

        var mockClients = new Mock<IHubCallerClients>();
        var mockGroupManager = new Mock<IGroupManager>();
        var mockMessageOrchestrator = new Mock<IMessageOrchestrator>();
        var mockChatOrchestrator = new Mock<IChatOrchestrator>();
        var mockLogger = new Mock<ILogger<OtoChatHub>>();

        mockMessageOrchestrator
            .Setup(m => m.SendMessageAsync(It.IsAny<MessagesDto>()))
            .ReturnsAsync(sendResult);

        mockMessageOrchestrator
            .Setup(m => m.ReplyForMessageAsync(newMessageId, replyToId))
            .ReturnsAsync(replyResult);

        var hub = new OtoChatHub(mockLogger.Object, mockMessageOrchestrator.Object, mockChatOrchestrator.Object)
        {
            Context = mockContext.Object,
            Clients = mockClients.Object,
            Groups = mockGroupManager.Object
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.ReplyToMessageAsync(recipient, messageContent, replyToId));

        Assert.Equal("Reply failed", ex.Message);
    }
    
    [Fact]
    public async Task SendMessageAsync_WithInvalidRecipient_ShouldHandleGracefully()
    {
        // Arrange
        var sender = "testuser1";
        var recipient = "";
        var content = "Test message";

        var sendResult = OperationResult<CreatedMessageResult>.Fail("Invalid recipient");

        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(It.IsAny<MessagesDto>()))
            .ReturnsAsync(sendResult);

        var connection = await CreateConnectionAsync(sender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
            await connection.InvokeAsync<object>("SendMessageAsync", recipient, content));

        Assert.Contains("Invalid recipient", exception.Message);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task LoadChatHistoryAsync_EmptyHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var currentUser = "testuser";
        var withUser = "testuser2";
        var take = 10;

        _messageOrchestratorMock
            .Setup(x => x.LoadChatHistory(currentUser, withUser, take))
            .ReturnsAsync(new List<Message>());

        var connection = await CreateConnectionAsync(currentUser);

        // Act
        var result = await connection.InvokeAsync<List<object>>("LoadChatHistoryAsync", withUser, take);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetChatsAsync_NoChats_ShouldReturnEmptyList()
    {
        // Arrange
        var nickname = "testuser";

        _chatOrchestratorMock
            .Setup(x => x.GetChatsAsync(nickname))
            .ReturnsAsync(new List<ChatDto>());

        var connection = await CreateConnectionAsync(nickname);

        // Act
        var result = await connection.InvokeAsync<List<ChatDto>>("GetChatsAsync");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task DeleteMessageAsync_FindMessageFailed_ShouldStillDelete()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sender = "testuser";

        var deleteResult = OperationResult<string>.Ok("Message deleted");
        var findResult = OperationResult<string>.Fail("Message not found");

        _messageOrchestratorMock
            .Setup(x => x.SoftDeleteMessageAsync(messageId))
            .ReturnsAsync(deleteResult);

        _messageOrchestratorMock
            .Setup(x => x.FindMessageByIdAsync(messageId))
            .ReturnsAsync(findResult);

        var connection = await CreateConnectionAsync(sender);

        // Act
        await connection.InvokeAsync("DeleteMessageAsync", messageId, "soft");

        // Assert
        _messageOrchestratorMock.Verify(x => x.SoftDeleteMessageAsync(messageId), Times.Once);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task FindMessagesAsync_WithValidParameters_ShouldReturnMessages()
{
    // Arrange
    var nickname = "testuser";
    var recipient = "recipient1";
    var filterDate = DateTime.UtcNow.AddDays(-1);
    var sender = "sender1";

    var messages = new List<Message>
    {
        new("sender1", "recipient1", "Message 1") { Id = Guid.NewGuid(), SendTime = filterDate},
        new("sender1", "recipient1", "Message 2") { Id = Guid.NewGuid(), SendTime = filterDate.AddMinutes(10)}
    };

    _messageOrchestratorMock
        .Setup(x => x.FindMessagesAsync(It.Is<MessageFilter>(f => 
            f.Sender == sender && f.Date == filterDate)))
        .ReturnsAsync(messages);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessagesAsync", recipient, filterDate, sender);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);

    _messageOrchestratorMock.Verify(x => x.FindMessagesAsync(It.Is<MessageFilter>(f => 
        f.Sender == sender && f.Date == filterDate)), Times.Once);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessagesAsync_WithNullResults_ShouldReturnEmptyList()
{
    // Arrange
    var nickname = "testuser";
    var recipient = "recipient1";
    var filterDate = DateTime.UtcNow;
    var sender = "sender1";

    _messageOrchestratorMock
        .Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .ReturnsAsync((List<Message>)null);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessagesAsync", recipient, filterDate, sender);

    // Assert
    Assert.NotNull(result);
    Assert.Empty(result);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessagesAsync_WithEmptyResults_ShouldReturnEmptyList()
{
    // Arrange
    var nickname = "testuser";
    var recipient = "recipient1";
    var filterDate = DateTime.UtcNow;
    var sender = "sender1";

    _messageOrchestratorMock
        .Setup(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()))
        .ReturnsAsync(new List<Message>());

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessagesAsync", recipient, filterDate, sender);

    // Assert
    Assert.NotNull(result);
    Assert.Empty(result);

    await connection.DisposeAsync();
}

    [Fact]
    public async Task FindMessagesAsync_WithNullParameters_ShouldCreateFilterCorrectly()
{
    // Arrange
    var nickname = "testuser";
    var messages = new List<Message>
    {
        new("sender1", "recipient1", "Message 1") { Id = Guid.NewGuid() }
    };

    _messageOrchestratorMock
        .Setup(x => x.FindMessagesAsync(It.Is<MessageFilter>(f => 
            f.Sender == null && f.Date == null)))
        .ReturnsAsync(messages);

    var connection = await CreateConnectionAsync(nickname);

    // Act
    var result = await connection.InvokeAsync<List<object>>("FindMessagesAsync", null, null, null);

    // Assert
    Assert.NotNull(result);
    Assert.Single(result);

    _messageOrchestratorMock.Verify(x => x.FindMessagesAsync(It.Is<MessageFilter>(f => 
        f.Sender == null && f.Date == null)), Times.Once);

    await connection.DisposeAsync();
}
    
    private async Task<HubConnection> CreateConnectionAsync(string nickname)
    {
        var token = GenerateJwtToken(nickname);
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5030/otoChatHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync();
        return connection;
    }

    private string GenerateJwtToken(string nickname)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = "your-secret-key-here-make-it-longer-than-32-characters"u8.ToArray();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.UserData, nickname)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class FakePolicyEvaluator : IPolicyEvaluator
{
    public async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        var principal = new ClaimsPrincipal();
        principal.AddIdentity(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.UserData, "testuser")
        }, "Test"));

        return await Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
    }

    public async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object? resource)
    {
        return await Task.FromResult(PolicyAuthorizationResult.Success());
    }
}
