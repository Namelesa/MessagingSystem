using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupMessages.Dto
{
    public class GroupMessageDtoTests
    {
        private const string TestSender = "testUser";
        private const string TestContent = "Hello, World!";
        private static readonly Guid TestGroupId = Guid.NewGuid();

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeProperties()
        {
            // Arrange & Act
            var before = DateTime.UtcNow;
            
            var dto = new GroupMessageDto(TestSender, TestContent, TestGroupId);

            // Assert
            Assert.Equal(TestSender, dto.Sender);
            Assert.Equal(TestContent, dto.Content);
            Assert.Equal(TestGroupId, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithNullSender_ShouldSetSenderToNull()
        {
            // Arrange & Act
            var before = DateTime.UtcNow;
            var dto = new GroupMessageDto(null, TestContent, TestGroupId);

            // Assert
            Assert.Null(dto.Sender);
            Assert.Equal(TestContent, dto.Content);
            Assert.Equal(TestGroupId, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithNullContent_ShouldSetContentToNull()
        {
            // Arrange & Act
            var before = DateTime.UtcNow;
            var dto = new GroupMessageDto(TestSender, null, TestGroupId);

            // Assert
            Assert.Equal(TestSender, dto.Sender);
            Assert.Null(dto.Content);
            Assert.Equal(TestGroupId, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithEmptyGuid_ShouldSetGroupIdToEmptyGuid()
        {
            // Arrange & Act
            var before = DateTime.UtcNow;
            var dto = new GroupMessageDto(TestSender, TestContent, Guid.Empty);

            // Assert
            Assert.Equal(TestSender, dto.Sender);
            Assert.Equal(TestContent, dto.Content);
            Assert.Equal(Guid.Empty, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Xunit.Theory]
        [InlineData("", "content", "00000000-0000-0000-0000-000000000000")]
        [InlineData("sender", "", "00000000-0000-0000-0000-000000000000")]
        [InlineData("sender", "content", "550e8400-e29b-41d4-a716-446655440000")]
        public void Constructor_WithVariousInputs_ShouldInitializeCorrectly(
            string sender, string content, string groupIdString)
        {
            // Arrange
            var groupId = Guid.Parse(groupIdString);
            var before = DateTime.UtcNow;

            // Act
            var dto = new GroupMessageDto(sender, content, groupId);

            // Assert
            Assert.Equal(sender, dto.Sender);
            Assert.Equal(content, dto.Content);
            Assert.Equal(groupId, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Fact]
        public void Properties_AreInitOnly_CannotBeSetAfterConstruction()
        {
            // Arrange
            var before = DateTime.UtcNow;
            var dto = new GroupMessageDto(TestSender, TestContent, TestGroupId);

            // Act & Assert
            Assert.Equal(TestSender, dto.Sender);
            Assert.Equal(TestContent, dto.Content);
            Assert.Equal(TestGroupId, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Fact]
        public void ObjectInitializer_WithInitProperties_ShouldWork()
        {
            // Arrange
            var newSender = "initializedSender";
            var newContent = "initializedContent";
            var newGroupId = Guid.NewGuid();
            var before = DateTime.UtcNow;

            // Act
            var dto = new GroupMessageDto(TestSender, TestContent, TestGroupId)
            {
                Sender = newSender,
                Content = newContent,
                GroupId = newGroupId
            };

            // Assert
            Assert.Equal(newSender, dto.Sender);
            Assert.Equal(newContent, dto.Content);
            Assert.Equal(newGroupId, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Fact]
        public void TwoInstances_WithSameValues_ShouldNotBeEqual()
        {
            // Arrange & Act
            var dto1 = new GroupMessageDto(TestSender, TestContent, TestGroupId);
            var dto2 = new GroupMessageDto(TestSender, TestContent, TestGroupId);

            // Assert
            Assert.NotEqual(dto1, dto2);
            Assert.False(dto1.Equals(dto2));
            Assert.NotSame(dto1, dto2);
        }

        [Fact]
        public void Properties_ShouldReturnExpectedTypes()
        {
            // Arrange & Act
            var dto = new GroupMessageDto(TestSender, TestContent, TestGroupId);

            // Assert
            Assert.IsType<string>(dto.Sender);
            Assert.IsType<string>(dto.Content);
            Assert.IsType<Guid>(dto.GroupId);
            Assert.IsType<DateTime>(dto.SendTime);
        }

        [Fact]
        public void Constructor_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var specialSender = "user@domain.com";
            var specialContent = "Message with 🚀 emojis and special chars: äöü";
            var before = DateTime.UtcNow;

            // Act
            var dto = new GroupMessageDto(specialSender, specialContent, TestGroupId);

            // Assert
            Assert.Equal(specialSender, dto.Sender);
            Assert.Equal(specialContent, dto.Content);
            Assert.Equal(TestGroupId, dto.GroupId);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithLongStrings_ShouldHandleCorrectly()
        {
            // Arrange
            var longSender = new string('a', 1000);
            var longContent = new string('b', 10000);
            var before = DateTime.UtcNow;

            // Act
            var dto = new GroupMessageDto(longSender, longContent, TestGroupId);

            // Assert
            Assert.Equal(longSender, dto.Sender);
            Assert.Equal(longContent, dto.Content);
            Assert.Equal(TestGroupId, dto.GroupId);
            Assert.Equal(1000, dto.Sender.Length);
            Assert.Equal(10000, dto.Content.Length);
            Assert.InRange(dto.SendTime, before, DateTime.UtcNow);
        }
        
        [Fact]
        public void GroupMessageDto_InitProperties_ShouldSetCorrectValues()
        {
            // Arrange
            var sender = "TestUser";
            var content = "Test message";
            var groupId = Guid.NewGuid();
            var customTime = DateTime.UtcNow.AddMinutes(-5);

            // Act
            var dto = new GroupMessageDto(sender, content, groupId)
            {
                SendTime = customTime
            };

            // Assert
            Assert.Equal(sender, dto.Sender);
            Assert.Equal(content, dto.Content);
            Assert.Equal(groupId, dto.GroupId);
            Assert.Equal(customTime, dto.SendTime);
        }

        [Fact]
        public void GroupMessageDto_DefaultSendTime_ShouldBeSetToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;
    
            // Act
            var dto = new GroupMessageDto("user", "message", Guid.NewGuid());
    
            // Assert
            var after = DateTime.UtcNow;
            Assert.True(dto.SendTime >= before && dto.SendTime <= after);
        }
    }
}