using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.Notification.Application.Messaging.Key;
using Moq;

namespace MessagingSystem.Tests.Notification.UnitTests.Application.Messaging.Key;

public class PublicKeyConsumerTests
{
    private readonly Mock<IDecryptionInfo> _decryptInfoMock;
    private readonly Mock<ConsumeContext<PublicKeyMessage>> _contextMock;
    private readonly PublicKeyConsumer _consumer;

    public PublicKeyConsumerTests()
    {
        _decryptInfoMock = new Mock<IDecryptionInfo>();
        _contextMock = new Mock<ConsumeContext<PublicKeyMessage>>();
        _consumer = new PublicKeyConsumer(_decryptInfoMock.Object);
    }

    [Fact]
    public async Task Consume_ServiceNameIsNotification_ShouldNotSaveKey()
    {
        // Arrange
        var encryptedServiceName = "encrypted";
        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = "someEncryptedKey"
        };

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedServiceName))
            .Returns("Notification");

        _contextMock.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _decryptInfoMock.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ServiceNameIsAnotherService_ShouldSaveDecryptedKeyToFile()
    {
        // Arrange
        var encryptedServiceName = "encryptedService";
        var encryptedPublicKey = "encryptedKey";
        var decryptedServiceName = "AnotherService";
        var decryptedPublicKey = "decrypted-key-content";

        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = encryptedPublicKey
        };

        _contextMock.Setup(x => x.Message).Returns(message);

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedServiceName))
            .Returns(decryptedServiceName);

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedPublicKey))
            .Returns(decryptedPublicKey);

        var baseDirectory = AppContext.BaseDirectory;
        var keyDirectory = Path.Combine(baseDirectory, "Key", decryptedServiceName);
        var keyPath = Path.Combine(keyDirectory, "public.key");
        
        if (Directory.Exists(keyDirectory))
            Directory.Delete(keyDirectory, true);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        Assert.True(File.Exists(keyPath));
        var fileContent = await File.ReadAllTextAsync(keyPath);
        Assert.Equal(decryptedPublicKey, fileContent);

        // Clean up
        Directory.Delete(keyDirectory, true);
    }

    [Fact]
    public async Task Consume_ShouldHandleNullOrEmptyServiceName()
    {
        // Arrange
        var encryptedServiceName = ""; 
        var encryptedPublicKey = "encryptedKey";
        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = encryptedPublicKey
        };

        _contextMock.Setup(x => x.Message).Returns(message);

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedServiceName))
            .Returns(string.Empty); 

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedPublicKey))
            .Returns("decrypted-public-key");

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _decryptInfoMock.Verify(x => x.Decrypt(encryptedServiceName), Times.Once); 
        _decryptInfoMock.Verify(x => x.Decrypt(encryptedPublicKey), Times.Once); 
    }


    [Fact]
    public async Task Consume_ShouldHandleDirectoryCreationError()
    {
        // Arrange
        var encryptedServiceName = "encryptedService";
        var encryptedPublicKey = "encryptedKey";
        var decryptedServiceName = "AnotherService";
        var decryptedPublicKey = "decrypted-key-content";

        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = encryptedPublicKey
        };

        _contextMock.Setup(x => x.Message).Returns(message);

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedServiceName))
            .Returns(decryptedServiceName);

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedPublicKey))
            .Returns(decryptedPublicKey);
        
        var baseDirectory = AppContext.BaseDirectory;
        Path.Combine(baseDirectory, "Key", decryptedServiceName);
        
        Directory.SetCurrentDirectory(Path.GetTempPath());

        // Act
        var exception = await Record.ExceptionAsync(() => _consumer.Consume(_contextMock.Object));

        // Assert
        Assert.Null(exception); 
    }

    [Fact]
    public async Task Consume_ShouldHandleFailedDecryptionOfPublicKey()
    {
        // Arrange
        var encryptedServiceName = "encryptedService";
        var encryptedPublicKey = "encryptedKey";
        var decryptedServiceName = "AnotherService";

        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = encryptedPublicKey
        };

        _contextMock.Setup(x => x.Message).Returns(message);
        
        _decryptInfoMock.Setup(x => x.Decrypt(encryptedServiceName))
            .Returns(decryptedServiceName);
        
        _decryptInfoMock.Setup(x => x.Decrypt(encryptedPublicKey))
            .Throws(new InvalidOperationException("Failed to decrypt public key"));

        // Act
        var exception = await Record.ExceptionAsync(() => _consumer.Consume(_contextMock.Object));

        // Assert
        Assert.NotNull(exception); 
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task Consume_ShouldNotSaveKey_WhenServiceNameIsNotification()
    {
        // Arrange
        var encryptedServiceName = "encrypted";
        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = "someEncryptedKey"
        };

        _decryptInfoMock.Setup(x => x.Decrypt(encryptedServiceName))
            .Returns("Notification");

        _contextMock.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        var baseDirectory = AppContext.BaseDirectory;
        var keyDirectory = Path.Combine(baseDirectory, "Key", "Notification");
        Assert.False(Directory.Exists(keyDirectory));
    }
}
