using MessagingSystem.Services.Messaging.Infrastructure.MessageBroker;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Infrastructure
{
    public class MessageBrokerSettingsTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializePropertiesWithEmptyStrings()
        {
            // Act
            var settings = new MessageBrokerSettings();

            // Assert
            Assert.Equal(string.Empty, settings.Host);
            Assert.Equal(string.Empty, settings.UserName);
            Assert.Equal(string.Empty, settings.Password);
        }

        [Fact]
        public void ObjectInitializer_ShouldAssignAllPropertiesCorrectly()
        {
            // Arrange
            var expectedHost = "localhost";
            var expectedUserName = "admin";
            var expectedPassword = "pass123";

            // Act
            var settings = new MessageBrokerSettings
            {
                Host = expectedHost,
                UserName = expectedUserName,
                Password = expectedPassword
            };

            // Assert
            Assert.Equal(expectedHost, settings.Host);
            Assert.Equal(expectedUserName, settings.UserName);
            Assert.Equal(expectedPassword, settings.Password);
        }
    }
}