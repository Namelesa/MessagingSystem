using MessagingSystem.SendingModels.PublicKey;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.PublicKey
{
    public class PublicKeyMessageTests
    {
        [Fact]
        public void Should_Create_PublicKeyMessage_With_Valid_Values()
        {
            // Arrange
            const string serviceName = "TestService";
            const string publicKey = "testPublicKey123";

            // Act
            var message = new PublicKeyMessage
            {
                ServiceName = serviceName,
                PublicKey = publicKey
            };

            // Assert
            Assert.Equal(serviceName, message.ServiceName);
            Assert.Equal(publicKey, message.PublicKey);
        }

        [Fact]
        public void Should_Handle_Empty_PublicKey_Gracefully()
        {
            // Arrange
            const string serviceName = "TestService";
            var publicKey = string.Empty;

            // Act
            var message = new PublicKeyMessage
            {
                ServiceName = serviceName,
                PublicKey = publicKey
            };

            // Assert
            Assert.Equal(serviceName, message.ServiceName);
            Assert.Equal(publicKey, message.PublicKey); 
        }

        [Fact]
        public void Should_Handle_Null_PublicKey_Gracefully()
        {
            // Arrange
            const string serviceName = "TestService";
            string? publicKey = null;

            // Act
            var message = new PublicKeyMessage
            {
                ServiceName = serviceName,
                PublicKey = publicKey
            };

            // Assert
            Assert.Equal(serviceName, message.ServiceName);
            Assert.Null(message.PublicKey);
        }
    }
}