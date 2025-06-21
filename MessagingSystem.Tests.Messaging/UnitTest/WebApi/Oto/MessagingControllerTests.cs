using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.WebApi.Messages;
using MessagingSystem.Services.Messaging.WebApi.Messages.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Oto;

public class MessagingControllerTests
{
    private readonly Mock<IUserOrchestrator> _userOrchestratorMock;
    private readonly Mock<IMessageOrchestrator> _messageOrchestratorMock;
    private readonly Mock<IChatOrchestrator> _chatOrchestratorMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly MessagingController _controller;

    public MessagingControllerTests()
    {
        _userOrchestratorMock = new Mock<IUserOrchestrator>();
        _messageOrchestratorMock = new Mock<IMessageOrchestrator>();
        _chatOrchestratorMock = new Mock<IChatOrchestrator>();
        _mapperMock = new Mock<IMapper>();

        _controller = new MessagingController(
            _userOrchestratorMock.Object,
            _messageOrchestratorMock.Object,
            _chatOrchestratorMock.Object,
            _mapperMock.Object);
    }

    #region CheckUserAsync Tests

    [Fact]
    public async Task CheckUserAsync_ValidNickname_ReturnsOkWithUser()
    {
        // Arrange
        var nickname = "testUser";
        var expectedUser = new FoundedUser("testUser", "Test User");
        var expectedResult = OperationResult<FoundedUser>.Ok(expectedUser);
        
        _userOrchestratorMock
            .Setup(x => x.CheckUserAsync(nickname))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CheckUserAsync(nickname);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var opResult = Assert.IsType<OperationResult<FoundedUser>>(okResult.Value);
        opResult.Success.Should().BeTrue();
        opResult.Data.Should().BeEquivalentTo(expectedUser);
        _userOrchestratorMock.Verify(x => x.CheckUserAsync(nickname), Times.Once);
    }

    [Fact]
    public async Task CheckUserAsync_UserOrchestratorThrowsException_ThrowsException()
    {
        // Arrange
        var nickname = "testUser";
        var expectedException = new Exception("User not found");
        
        _userOrchestratorMock
            .Setup(x => x.CheckUserAsync(nickname))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _controller.CheckUserAsync(nickname));
        Assert.Equal(expectedException.Message, exception.Message);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public void GetCurrentUser_ValidUserDataClaim_ReturnsOkWithNickname()
    {
        // Arrange
        var nickname = "testUser";
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, nickname)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = okResult.Value;
        
        var nickProperty = returnValue?.GetType().GetProperty("nick");
        Assert.NotNull(nickProperty);
        Assert.Equal(nickname, nickProperty.GetValue(returnValue));
    }

    [Fact]
    public void GetCurrentUser_NoUserDataClaim_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testUser") 
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Nickname not found in token", unauthorizedResult.Value);
    }

    [Fact]
    public void GetCurrentUser_EmptyUserDataClaim_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, "") 
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Nickname not found in token", unauthorizedResult.Value);
    }

    [Fact]
    public void GetCurrentUser_NullUserDataClaim_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, "")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Nickname not found in token", unauthorizedResult.Value);
    }

    [Fact]
    public void GetCurrentUser_NoClaims_ReturnsUnauthorized()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Nickname not found in token", unauthorizedResult.Value);
    }

    #endregion

    #region FindMessageAsync Tests

    [Fact]
    public async Task FindMessageAsync_ValidFilter_ReturnsOkWithMessages()
    {
        // Arrange
        var filter = new MessageFilter();
        var expectedMessages = new List<Message> { new Message("sender", "recipient", "content") };
        
        _messageOrchestratorMock
            .Setup(x => x.FindMessagesAsync(filter))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.FindMessageAsync(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedMessages, okResult.Value);
        _messageOrchestratorMock.Verify(x => x.FindMessagesAsync(filter), Times.Once);
    }

    [Fact]
    public async Task FindMessageAsync_MessageOrchestratorThrowsException_ThrowsException()
    {
        // Arrange
        var filter = new MessageFilter();
        var expectedException = new Exception("Database error");
        
        _messageOrchestratorMock
            .Setup(x => x.FindMessagesAsync(filter))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _controller.FindMessageAsync(filter));
        Assert.Equal(expectedException.Message, exception.Message);
    }

    [Fact]
    public async Task FindMessageAsync_EmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var filter = new MessageFilter();
        var emptyMessages = new List<Message>();
        
        _messageOrchestratorMock
            .Setup(x => x.FindMessagesAsync(filter))
            .ReturnsAsync(emptyMessages);

        // Act
        var result = await _controller.FindMessageAsync(filter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(emptyMessages, okResult.Value);
    }

    #endregion

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_ValidMessage_ReturnsOkWithResult()
    {
        // Arrange
        var createMessage = new CreateMessage("sender", "receiver", "Hello");
        var messageDto = new MessagesDto("sender", "receiver", "Hello");
        var expectedInner = new CreatedMessageResult(Guid.NewGuid(), DateTime.UtcNow);
        var expectedResult = OperationResult<CreatedMessageResult>.Ok(expectedInner);


        _mapperMock
            .Setup(x => x.Map<MessagesDto>(createMessage))
            .Returns(messageDto);

        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(messageDto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SendMessageAsync(createMessage);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualValue = Assert.IsType<OperationResult<CreatedMessageResult>>(okResult.Value);
        actualValue.Should().BeEquivalentTo(expectedResult, options => options
            .ComparingByMembers<CreatedMessageResult>());

        _mapperMock.Verify(x => x.Map<MessagesDto>(createMessage), Times.Once);
        _messageOrchestratorMock.Verify(x => x.SendMessageAsync(messageDto), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_MapperThrowsException_ThrowsException()
    {
        // Arrange
        var createMessage = new CreateMessage("sender", "receiver", "Hello");
        var expectedException = new Exception("Mapping error");
        
        _mapperMock
            .Setup(x => x.Map<MessagesDto>(createMessage))
            .Throws(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _controller.SendMessageAsync(createMessage));
        Assert.Equal(expectedException.Message, exception.Message);
        
        _mapperMock.Verify(x => x.Map<MessagesDto>(createMessage), Times.Once);
        _messageOrchestratorMock.Verify(x => x.SendMessageAsync(It.IsAny<MessagesDto>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_MessageOrchestratorThrowsException_ThrowsException()
    {
        // Arrange
        var createMessage = new CreateMessage("sender", "receiver", "Hello");
        var messageDto = new MessagesDto("sender", "receiver", "Hello");
        var expectedException = new Exception("Send message error");
        
        _mapperMock
            .Setup(x => x.Map<MessagesDto>(createMessage))
            .Returns(messageDto);
        
        _messageOrchestratorMock
            .Setup(x => x.SendMessageAsync(messageDto))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _controller.SendMessageAsync(createMessage));
        Assert.Equal(expectedException.Message, exception.Message);
        
        _mapperMock.Verify(x => x.Map<MessagesDto>(createMessage), Times.Once);
        _messageOrchestratorMock.Verify(x => x.SendMessageAsync(messageDto), Times.Once);
    }

    #endregion
    
    [Fact]
    public async Task GetChatsAsync_ReturnsOkWithChats()
    {
        // Arrange
        var nickName = "user1";
        var expectedChats = new List<ChatDto>
        {
            new ChatDto { NickName = "user2", Image = "image1" },
            new ChatDto { NickName = "user3", Image = "image2" }
        };

        _chatOrchestratorMock
            .Setup(x => x.GetChatsAsync(nickName))
            .ReturnsAsync(expectedChats);

        // Act
        var result = await _controller.GetChatsAsync(nickName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        var returnedChats = Assert.IsAssignableFrom<IEnumerable<ChatDto>>(okResult.Value);
        Assert.Equal(expectedChats, returnedChats);
    }

    [Fact]
    public async Task GetChatsAsync_WhenChatOrchestratorReturnsNull_ReturnsOkWithNull()
    {
        // Arrange
        var nickName = "user1";

        _chatOrchestratorMock
            .Setup(x => x.GetChatsAsync(nickName))
            .ReturnsAsync((List<ChatDto>?)null);

        // Act
        var result = await _controller.GetChatsAsync(nickName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
    }
}