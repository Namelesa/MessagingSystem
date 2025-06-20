using AutoMapper;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Group
{
    public class GroupMessagesControllerTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IGroupMessagesOrchestrator> _orchestratorMock;
        private readonly GroupMessagesController _controller;

        public GroupMessagesControllerTests()
        {
            _mapperMock = new Mock<IMapper>();
            _orchestratorMock = new Mock<IGroupMessagesOrchestrator>();
            _controller = new GroupMessagesController(_mapperMock.Object, _orchestratorMock.Object);
        }

        #region SendMessageAsync Tests
        [Fact]
        public async Task SendMessageAsync_ValidMessage_ReturnsOkResult()
        {
            // Arrange
            var createMessage = new CreateGroupMessage("senderId", "receiverId", Guid.NewGuid());
            var messageDto = new GroupMessageDto("senderId", "receiverId", Guid.NewGuid());
            var expectedInner = new CreatedMessageResult(Guid.NewGuid(), DateTime.UtcNow);
            var expectedResult = OperationResult<CreatedMessageResult>.Ok(expectedInner);

            _mapperMock.Setup(m => m.Map<GroupMessageDto>(createMessage))
                      .Returns(messageDto);
            _orchestratorMock.Setup(o => o.SendMessageAsync(messageDto))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SendMessageAsync(createMessage);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _mapperMock.Verify(m => m.Map<GroupMessageDto>(createMessage), Times.Once);
            _orchestratorMock.Verify(o => o.SendMessageAsync(messageDto), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_OrchestratorThrowsException_ThrowsException()
        {
            // Arrange
            var createMessage = new CreateGroupMessage("senderId", "receiverId", Guid.NewGuid());
            var messageDto = new GroupMessageDto("senderId", "receiverId", Guid.NewGuid());
            var expectedException = new InvalidOperationException("Test exception");

            _mapperMock.Setup(m => m.Map<GroupMessageDto>(createMessage))
                      .Returns(messageDto);
            _orchestratorMock.Setup(o => o.SendMessageAsync(messageDto))
                           .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.SendMessageAsync(createMessage));
            Assert.Equal("Test exception", exception.Message);
        }
        #endregion

        #region FindMessageAsync Tests
        [Fact]
        public async Task FindMessageAsync_ValidFilter_ReturnsOkResult()
        {
            // Arrange
            var filter = new MessageFilter();
            var expectedMessages = new List<GroupMessage> 
            { 
                new GroupMessage("sender1", "content1"),
                new GroupMessage("sender2", "content2")
            };
            var expectedResult = new List<GroupMessage>();

            _orchestratorMock.Setup(o => o.FindMessagesAsync(filter))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.FindMessageAsync(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.FindMessagesAsync(filter), Times.Once);
        }

        [Fact]
        public async Task FindMessageAsync_OrchestratorThrowsException_ThrowsException()
        {
            // Arrange
            var filter = new MessageFilter();
            var expectedException = new ArgumentException("Invalid filter");

            _orchestratorMock.Setup(o => o.FindMessagesAsync(filter))
                           .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _controller.FindMessageAsync(filter));
            Assert.Equal("Invalid filter", exception.Message);
        }
        #endregion

        #region SoftDeleteMessageAsync Tests
        [Fact]
        public async Task SoftDeleteMessageAsync_ValidMessageId_ReturnsOkResult()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var expectedResult = OperationResult<string>.Ok("Message soft deleted successfully");

            _orchestratorMock.Setup(o => o.SoftDeleteMessageAsync(messageId))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SoftDeleteMessageAsync(messageId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.SoftDeleteMessageAsync(messageId), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteMessageAsync_EmptyGuid_CallsOrchestrator()
        {
            // Arrange
            var messageId = Guid.Empty;
            var expectedResult = OperationResult<string>.Ok("Message soft deleted successfully");

            _orchestratorMock.Setup(o => o.SoftDeleteMessageAsync(messageId))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SoftDeleteMessageAsync(messageId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.SoftDeleteMessageAsync(messageId), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteMessageAsync_OrchestratorThrowsException_ThrowsException()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var expectedException = new InvalidOperationException("Soft delete failed");

            _orchestratorMock.Setup(o => o.SoftDeleteMessageAsync(messageId))
                           .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.SoftDeleteMessageAsync(messageId));
            Assert.Equal("Soft delete failed", exception.Message);
        }
        #endregion

        #region DeleteMessageAsync Tests
        [Fact]
        public async Task DeleteMessageAsync_ValidMessageId_ReturnsOkResult()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var expectedResult = OperationResult<string>.Ok("Message deleted successfully");

            _orchestratorMock.Setup(o => o.DeleteMessageAsync(messageId))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.DeleteMessageAsync(messageId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.DeleteMessageAsync(messageId), Times.Once);
        }

        [Fact]
        public async Task DeleteMessageAsync_OrchestratorThrowsException_ThrowsException()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var expectedException = new UnauthorizedAccessException("Access denied");

            _orchestratorMock.Setup(o => o.DeleteMessageAsync(messageId))
                           .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _controller.DeleteMessageAsync(messageId));
            Assert.Equal("Access denied", exception.Message);
        }

        [Fact]
        public async Task DeleteMessageAsync_EmptyGuid_CallsOrchestrator()
        {
            // Arrange
            var messageId = Guid.Empty;
            var expectedResult = OperationResult<string>.Ok("Message deleted successfully");

            _orchestratorMock.Setup(o => o.DeleteMessageAsync(messageId))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.DeleteMessageAsync(messageId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.DeleteMessageAsync(messageId), Times.Once);
        }
        #endregion

        #region FindMessageByIdAsync Tests
        [Fact]
        public async Task FindMessageByIdAsync_ValidMessageId_ReturnsOkResult()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var expectedMessage = new GroupMessageDto("sender", "content", Guid.NewGuid());
            var expectedResult = OperationResult<string>.Ok(expectedMessage.Sender);

            _orchestratorMock.Setup(o => o.FindMessageByIdAsync(messageId))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.FindMessageByIdAsync(messageId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var operationResult = Assert.IsType<OperationResult<string>>(okResult.Value);
            Assert.Equal(expectedMessage.Sender, operationResult.Data);
            _orchestratorMock.Verify(o => o.FindMessageByIdAsync(messageId), Times.Once);
        }

        [Fact]
        public async Task FindMessageByIdAsync_MessageNotFound_ReturnsOkWithEmptyData()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var expectedResult = OperationResult<string>.Ok(null);

            _orchestratorMock.Setup(o => o.FindMessageByIdAsync(messageId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.FindMessageByIdAsync(messageId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var operationResult = Assert.IsAssignableFrom<OperationResult<string>>(okResult.Value);
            Assert.Null(operationResult.Data);
            _orchestratorMock.Verify(o => o.FindMessageByIdAsync(messageId), Times.Once);
        }

        [Fact]
        public async Task FindMessageByIdAsync_OrchestratorThrowsException_ThrowsException()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var expectedException = new KeyNotFoundException("Message not found");

            _orchestratorMock.Setup(o => o.FindMessageByIdAsync(messageId))
                           .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _controller.FindMessageByIdAsync(messageId));
            Assert.Equal("Message not found", exception.Message);
        }
        #endregion

        #region LoadChatMessagesAsync Tests
        [Fact]
        public async Task LoadChatMessagesAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var take = 50;
            var expectedMessages = new List<GroupMessage>
            {
                new GroupMessage("sender1", "content1"),
                new GroupMessage("sender2", "content2")
            };
            
            _orchestratorMock.Setup(o => o.LoadChatHistory(groupId, take))
                           .ReturnsAsync(expectedMessages);

            // Act
            var result = await _controller.LoadChatMessagesAsync(groupId, take);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedMessages, okResult.Value);
            _orchestratorMock.Verify(o => o.LoadChatHistory(groupId, take), Times.Once);
        }

        [Fact]
        public async Task LoadChatMessagesAsync_EmptyGroupId_CallsOrchestrator()
        {
            // Arrange
            var groupId = Guid.Empty;
            var take = 10;
            var expectedResult = new List<GroupMessage>();

            _orchestratorMock.Setup(o => o.LoadChatHistory(groupId, take))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.LoadChatMessagesAsync(groupId, take);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.LoadChatHistory(groupId, take), Times.Once);
        }

        [Fact]
        public async Task LoadChatMessagesAsync_ZeroTake_CallsOrchestrator()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var take = 0;
            var expectedResult = new List<GroupMessage>();

            _orchestratorMock.Setup(o => o.LoadChatHistory(groupId, take))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.LoadChatMessagesAsync(groupId, take);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.LoadChatHistory(groupId, take), Times.Once);
        }

        [Fact]
        public async Task LoadChatMessagesAsync_NegativeTake_CallsOrchestrator()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var take = -5;
            var expectedResult = new List<GroupMessage>();

            _orchestratorMock.Setup(o => o.LoadChatHistory(groupId, take))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.LoadChatMessagesAsync(groupId, take);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.LoadChatHistory(groupId, take), Times.Once);
        }

        [Fact]
        public async Task LoadChatMessagesAsync_OrchestratorThrowsException_ThrowsException()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var take = 50;
            var expectedException = new ArgumentException("Invalid group ID");

            _orchestratorMock.Setup(o => o.LoadChatHistory(groupId, take))
                           .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _controller.LoadChatMessagesAsync(groupId, take));
            Assert.Equal("Invalid group ID", exception.Message);
        }
        #endregion

        #region ReplyMessageAsync Tests
        [Fact]
        public async Task ReplyMessageAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var replyId = Guid.NewGuid();
            var expectedResult = OperationResult<GroupMessage>.Ok(new GroupMessage("sender", "content"));

            _orchestratorMock.Setup(o => o.ReplyForMessageAsync(messageId, replyId))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ReplyMessageAsync(messageId, replyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.ReplyForMessageAsync(messageId, replyId), Times.Once);
        }

        [Fact]
        public async Task ReplyMessageAsync_EmptyGuids_CallsOrchestrator()
        {
            // Arrange
            var messageId = Guid.Empty;
            var replyId = Guid.Empty;
            var expectedResult = OperationResult<GroupMessage>.Ok(new GroupMessage("sender", "content"));

            _orchestratorMock.Setup(o => o.ReplyForMessageAsync(messageId, replyId))
                           .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ReplyMessageAsync(messageId, replyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
            _orchestratorMock.Verify(o => o.ReplyForMessageAsync(messageId, replyId), Times.Once);
        }

        [Fact]
        public async Task ReplyMessageAsync_OrchestratorThrowsException_ThrowsException()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var replyId = Guid.NewGuid();
            var expectedException = new InvalidOperationException("Reply operation failed");

            _orchestratorMock.Setup(o => o.ReplyForMessageAsync(messageId, replyId))
                           .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.ReplyMessageAsync(messageId, replyId));
            Assert.Equal("Reply operation failed", exception.Message);
        }
        #endregion

        #region Constructor Tests
        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var controller = new GroupMessagesController(_mapperMock.Object, _orchestratorMock.Object);

            // Assert
            Assert.NotNull(controller);
        }
        #endregion
    }
}