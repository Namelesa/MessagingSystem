using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages.Contracts;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Group
{
    public class CreateGroupMessageTests
    {
        [Fact]
        public void Constructor_ValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var sender = "User@123";
            var content = "Hello, group!";
            var groupId = Guid.NewGuid();

            // Act
            var message = new CreateGroupMessage(sender, content, groupId);

            // Assert
            Assert.Equal(sender, message.Sender);
            Assert.Equal(content, message.Content);
            Assert.Equal(groupId, message.GroupId);
        }

        [Fact]
        public void Sender_RequiredValidation_FailsForNullOrEmpty()
        {
            // Arrange
            var message = new CreateGroupMessage("", "Hello", Guid.NewGuid());

            // Act
            var results = ValidateModel(message);

            // Assert
            Assert.Contains(results, v =>
                v.MemberNames.Contains(nameof(CreateGroupMessage.Sender)) &&
                v.ErrorMessage == "Sender is required");
        }

        [Xunit.Theory]
        [InlineData("abc")]        
        [InlineData("abc@123456789012345")] 
        [InlineData("abc1234567890")] 
        public void Sender_RegexValidation_FailsIfNoSpecialCharOrInvalidLength(string sender)
        {
            // Arrange
            var message = new CreateGroupMessage(sender, "Hi", Guid.NewGuid());

            // Act
            var results = ValidateModel(message);

            // Assert
            Assert.Contains(results, v =>
                v.MemberNames.Contains(nameof(CreateGroupMessage.Sender)) &&
                v.ErrorMessage!.Contains("Sender name must be 3 to 15 characters long"));
        }
        
        [Xunit.Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Content_RequiredValidation_FailsForNullOrEmpty(string content)
        {
            // Arrange
            var message = new CreateGroupMessage("User@1", content!, Guid.NewGuid());

            // Act
            var results = ValidateModel(message);

            // Assert
            Assert.Contains(results, v =>
                v.MemberNames.Contains(nameof(CreateGroupMessage.Content)) &&
                v.ErrorMessage == "Content is required");
        }

        [Xunit.Theory]
        [InlineData(1901)]
        public void Content_LengthValidation_FailsForInvalidLength(int length)
        {
            // Arrange
            var content = new string('A', length);
            var message = new CreateGroupMessage("User@1", content, Guid.NewGuid());

            // Act
            var results = ValidateModel(message);

            // Assert
            Assert.Contains(results, v =>
                v.MemberNames.Contains(nameof(CreateGroupMessage.Content)) &&
                v.ErrorMessage == "Content length must be between 1 and 1900 characters.");
        }
        
        [Fact]
        public void GroupId_IsSetCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var message = new CreateGroupMessage("User@1", "Test", id);

            // Assert
            Assert.Equal(id, message.GroupId);
        }
        
        [Fact]
        public void Properties_HaveInitOnlySetters()
        {
            var type = typeof(CreateGroupMessage);
            var expectedInitProperties = new[] { "Sender", "Content", "GroupId" };

            foreach (var name in expectedInitProperties)
            {
                var prop = type.GetProperty(name);
                Assert.NotNull(prop);
                
                var backingField = type.GetField($"<{name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(backingField);
                
                var isInitOnly = backingField.IsInitOnly;
                Assert.True(isInitOnly, $"{name} is not init-only");
            }
        }
        
        [Fact]
        public void Constructor_WithNullSender_SetsPropertyToNull()
        {
            // Arrange & Act
            var message = new CreateGroupMessage(null!, "Content", Guid.NewGuid());

            // Assert
            Assert.Null(message.Sender);
        }

        [Fact]
        public void Constructor_WithNullContent_SetsPropertyToNull()
        { 
            // Arrange & Act
            var message = new CreateGroupMessage("User@1", null!, Guid.NewGuid());

            // Assert
            Assert.Null(message.Content);
        }

        [Fact]
        public void Constructor_WithEmptyGuid_SetsPropertyCorrectly()
        {
            // Arrange
            var emptyGuid = Guid.Empty;

            // Act
            var message = new CreateGroupMessage("User@1", "Content", emptyGuid);

            // Assert
            Assert.Equal(emptyGuid, message.GroupId);
        }

        [Fact]
        public void Constructor_AllParametersNull_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var message = new CreateGroupMessage(null!, null!, Guid.Empty);

            // Assert
            Assert.Null(message.Sender);
            Assert.Null(message.Content);
            Assert.Equal(Guid.Empty, message.GroupId);
        }

        [Fact]
        public void Properties_CanBeSetOnlyDuringInitialization()
        {
            // Arrange
            var sender = "Test@User";
            var content = "Test Content";
            var groupId = Guid.NewGuid();

            // Act
            var message = new CreateGroupMessage(sender, content, groupId)
            {
                Sender = sender,
                Content = content,
                GroupId = groupId
            };

            // Assert
            Assert.Equal(sender, message.Sender);
            Assert.Equal(content, message.Content);
            Assert.Equal(groupId, message.GroupId);
        }

        [Xunit.Theory]
        [InlineData("", "", "00000000-0000-0000-0000-000000000000")]
        [InlineData("a", "b", "11111111-1111-1111-1111-111111111111")]
        public void Constructor_WithVariousParameterCombinations_SetsPropertiesCorrectly(
            string sender, string content, string guidString)
        {
            // Arrange
            var groupId = Guid.Parse(guidString);

            // Act
            var message = new CreateGroupMessage(sender, content, groupId);

            // Assert
            Assert.Equal(sender, message.Sender);
            Assert.Equal(content, message.Content);
            Assert.Equal(groupId, message.GroupId);
        }

        
        private static List<ValidationResult> ValidateModel(object model)
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }
    }
}
