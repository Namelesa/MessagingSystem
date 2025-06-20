using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.Messaging.Application.MessageBroker.Key;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.MessageBroker
{
    public class PublicKeyConsumerTests : IDisposable
    {
        private readonly Mock<IDecryptionInfo> _mockDecryptionInfo;
        private readonly Mock<IPublicKeyStorage> _mockPublicKeyStorage;
        private readonly Mock<ConsumeContext<PublicKeyMessage>> _mockConsumeContext;
        private readonly PublicKeyConsumer _consumer;
        private readonly string _baseDirectory;

        public PublicKeyConsumerTests()
        {
            _mockDecryptionInfo = new Mock<IDecryptionInfo>();
            _mockPublicKeyStorage = new Mock<IPublicKeyStorage>();
            _mockConsumeContext = new Mock<ConsumeContext<PublicKeyMessage>>();
            _consumer = new PublicKeyConsumer(_mockDecryptionInfo.Object, _mockPublicKeyStorage.Object);
            
            _baseDirectory = AppContext.BaseDirectory;
        }

        [Fact]
        public async Task Consume_WhenServiceNameIsMessaging_ShouldReturnEarlyWithoutProcessing()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_messaging",
                PublicKey = "encrypted_public_key"
            };

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_messaging")).Returns("Messaging");

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockDecryptionInfo.Verify(x => x.Decrypt("encrypted_messaging"), Times.Once);
            _mockDecryptionInfo.Verify(x => x.Decrypt("encrypted_public_key"), Times.Never);
            _mockPublicKeyStorage.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Consume_WhenServiceNameIsNotMessaging_ShouldProcessPublicKey()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_auth_service",
                PublicKey = "encrypted_public_key_content"
            };

            var decryptedServiceName = "AuthService";
            var decryptedPublicKey = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----";

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_auth_service")).Returns(decryptedServiceName);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_public_key_content")).Returns(decryptedPublicKey);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockDecryptionInfo.Verify(x => x.Decrypt("encrypted_auth_service"), Times.Once);
            _mockDecryptionInfo.Verify(x => x.Decrypt("encrypted_public_key_content"), Times.Once);
            _mockPublicKeyStorage.Verify(x => x.Save(decryptedServiceName, decryptedPublicKey), Times.Once);
        }

        [Fact]
        public async Task Consume_WhenServiceNameIsNotMessaging_ShouldCreateServiceFolder()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_user_service",
                PublicKey = "encrypted_key_data"
            };

            var decryptedServiceName = $"UserService_{Guid.NewGuid():N}";
            var decryptedPublicKey = "public_key_content";

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_user_service")).Returns(decryptedServiceName);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_key_data")).Returns(decryptedPublicKey);

            try
            {
                // Act
                await _consumer.Consume(_mockConsumeContext.Object);

                // Assert
                var expectedFolderPath = Path.Combine(_baseDirectory, "Key", decryptedServiceName);
                Assert.True(Directory.Exists(expectedFolderPath));
            }
            finally
            {
                // Cleanup
                var testFolderPath = Path.Combine(_baseDirectory, "Key", decryptedServiceName);
                if (Directory.Exists(testFolderPath))
                {
                    Directory.Delete(testFolderPath, true);
                }
            }
        }

        [Fact]
        public async Task Consume_WhenServiceNameIsNotMessaging_ShouldSavePublicKeyToFile()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_notification_service",
                PublicKey = "encrypted_notification_key"
            };

            var decryptedServiceName = $"NotificationService_{Guid.NewGuid():N}";
            var decryptedPublicKey = "notification_public_key_content";

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_notification_service")).Returns(decryptedServiceName);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_notification_key")).Returns(decryptedPublicKey);

            try
            {
                // Act
                await _consumer.Consume(_mockConsumeContext.Object);

                // Assert
                var expectedFilePath = Path.Combine(_baseDirectory, "Key", decryptedServiceName, "public.key");
                Assert.True(File.Exists(expectedFilePath));
                
                var savedContent = await File.ReadAllTextAsync(expectedFilePath);
                Assert.Equal(decryptedPublicKey, savedContent);
            }
            finally
            {
                // Cleanup
                var testFolderPath = Path.Combine(_baseDirectory, "Key", decryptedServiceName);
                if (Directory.Exists(testFolderPath))
                {
                    Directory.Delete(testFolderPath, true);
                }
            }
        }

        [Fact]
        public async Task Consume_WhenServiceFolderAlreadyExists_ShouldOverwriteExistingKeyFile()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_payment_service",
                PublicKey = "encrypted_payment_key"
            };

            var decryptedServiceName = $"PaymentService_{Guid.NewGuid():N}";
            var oldPublicKey = "old_payment_key_content";
            var newPublicKey = "new_payment_key_content";
            
            var serviceFolderPath = Path.Combine(_baseDirectory, "Key", decryptedServiceName);
            var keyFilePath = Path.Combine(serviceFolderPath, "public.key");
            Directory.CreateDirectory(serviceFolderPath);
            await File.WriteAllTextAsync(keyFilePath, oldPublicKey);

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_payment_service")).Returns(decryptedServiceName);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_payment_key")).Returns(newPublicKey);

            try
            {
                // Act
                await _consumer.Consume(_mockConsumeContext.Object);

                // Assert
                Assert.True(File.Exists(keyFilePath));
                var savedContent = await File.ReadAllTextAsync(keyFilePath);
                Assert.Equal(newPublicKey, savedContent);
                Assert.NotEqual(oldPublicKey, savedContent);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(serviceFolderPath))
                {
                    Directory.Delete(serviceFolderPath, true);
                }
            }
        }

        [Xunit.Theory]
        [InlineData("messaging")]
        [InlineData("MESSAGING")]
        [InlineData("Messaging")]
        public async Task Consume_WhenDecryptedServiceNameIsMessagingInDifferentCases_ShouldReturnEarly(string serviceName)
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_service",
                PublicKey = "encrypted_key"
            };

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_service")).Returns(serviceName);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            if (serviceName == "Messaging")
            {
                _mockDecryptionInfo.Verify(x => x.Decrypt("encrypted_key"), Times.Never);
                _mockPublicKeyStorage.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
            else
            {
                _mockDecryptionInfo.Verify(x => x.Decrypt("encrypted_key"), Times.Once);
                _mockPublicKeyStorage.Verify(x => x.Save(serviceName, It.IsAny<string>()), Times.Once);
            }
        }

        [Fact]
        public async Task Consume_WhenDecryptionInfoThrowsException_ShouldPropagateException()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_service",
                PublicKey = "encrypted_key"
            };

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_service"))
                .Throws(new InvalidOperationException("Decryption failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _consumer.Consume(_mockConsumeContext.Object));
        }

        [Fact]
        public async Task Consume_WhenPublicKeyStorageThrowsException_ShouldPropagateException()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_service",
                PublicKey = "encrypted_key"
            };

            var decryptedServiceName = "TestService";
            var decryptedPublicKey = "test_public_key";

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_service")).Returns(decryptedServiceName);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_key")).Returns(decryptedPublicKey);
            _mockPublicKeyStorage.Setup(x => x.Save(decryptedServiceName, decryptedPublicKey))
                .Throws(new InvalidOperationException("Storage failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _consumer.Consume(_mockConsumeContext.Object));
        }

        [Fact]
        public async Task Consume_WithEmptyServiceName_ShouldHandleGracefully()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_empty",
                PublicKey = "encrypted_key"
            };

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_empty")).Returns(string.Empty);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_key")).Returns("some_key");

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockPublicKeyStorage.Verify(x => x.Save(string.Empty, "some_key"), Times.Once);
        }

        [Fact]
        public async Task Consume_WithNullServiceName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var message = new PublicKeyMessage
            {
                ServiceName = "encrypted_null",
                PublicKey = "encrypted_key"
            };

            _mockConsumeContext.Setup(x => x.Message).Returns(message);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_null")).Returns((string)null!);
            _mockDecryptionInfo.Setup(x => x.Decrypt("encrypted_key")).Returns("some_key");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _consumer.Consume(_mockConsumeContext.Object));
        }

        public void Dispose()
        {
            
        }
    }
}