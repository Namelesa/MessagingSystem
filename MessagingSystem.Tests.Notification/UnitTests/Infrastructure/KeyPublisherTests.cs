using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.Notification.Infrastructure.Key;
using Moq;

namespace MessagingSystem.Tests.Notification.UnitTests.Infrastructure
{
    public class KeyPublisherTests
    {
        [Fact]
        public async Task PublishAsync_ShouldPublishEncryptedPublicKeyMessage()
        {
            // Arrange
            var busMock = new Mock<IBus>();
            var encryptionInfoMock = new Mock<IEncryptionInfo>();

            var fakePublicKey = "plain-public-key";
            var encryptedPublicKey = "encrypted-public-key";
            var encryptedServiceName = "encrypted-user";

            encryptionInfoMock.Setup(e => e.GetPublicKey()).Returns(fakePublicKey);
            encryptionInfoMock.Setup(e => e.Encrypt(fakePublicKey)).Returns(encryptedPublicKey);
            encryptionInfoMock.Setup(e => e.Encrypt("Notification")).Returns(encryptedServiceName);

            var keyPublisher = new KeyPublisher(busMock.Object, encryptionInfoMock.Object);

            // Act
            await keyPublisher.PublishAsync();

            // Assert
            busMock.Verify(b => b.Publish(
                It.Is<PublicKeyMessage>(msg =>
                    msg.PublicKey == encryptedPublicKey &&
                    msg.ServiceName == encryptedServiceName),
                default), Times.Once);
        }
    }
}