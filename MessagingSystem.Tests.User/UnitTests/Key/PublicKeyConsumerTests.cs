using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.User.Application.Messaging.Key;
using MessagingSystem.Services.User.Infrastructure.Keys;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Key;

public class PublicKeyConsumerTests
{
    private readonly Mock<IDecryptionInfo> _decryptInfoMock;
    private readonly Mock<IPublicKeyStorage> _storageMock;
    private readonly PublicKeyConsumer _consumer;

    public PublicKeyConsumerTests()
    {
        _decryptInfoMock = new Mock<IDecryptionInfo>();
        _storageMock = new Mock<IPublicKeyStorage>();
        _consumer = new PublicKeyConsumer(_decryptInfoMock.Object, _storageMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldDecryptServiceNameAndIgnoreUserService()
    {
        // Arrange
        const string encryptedServiceName = "encryptedServiceName";
        const string decryptedServiceName = "User";
        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = "publicKey"
        };

        _decryptInfoMock.Setup(d => d.Decrypt(encryptedServiceName)).Returns(decryptedServiceName);

        var contextMock = new Mock<ConsumeContext<PublicKeyMessage>>();
        contextMock.Setup(c => c.Message).Returns(message);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _decryptInfoMock.Verify(d => d.Decrypt(encryptedServiceName), Times.Once);
        _storageMock.Verify(s => s.Save(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ShouldDecryptAndSavePublicKey_WhenServiceIsNotUser()
    {
        // Arrange
        const string encryptedServiceName = "encryptedServiceName";
        const string decryptedServiceName = "OtherService";
        const string encryptedPublicKey = "encryptedPublicKey";
        const string decryptedPublicKey = "decryptedPublicKey";

        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = encryptedPublicKey
        };

        _decryptInfoMock.Setup(d => d.Decrypt(encryptedServiceName)).Returns(decryptedServiceName);
        _decryptInfoMock.Setup(d => d.Decrypt(encryptedPublicKey)).Returns(decryptedPublicKey);

        var contextMock = new Mock<ConsumeContext<PublicKeyMessage>>();
        contextMock.Setup(c => c.Message).Returns(message);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _decryptInfoMock.Verify(d => d.Decrypt(encryptedServiceName), Times.Once);
        _decryptInfoMock.Verify(d => d.Decrypt(encryptedPublicKey), Times.Once);
        _storageMock.Verify(s => s.Save(decryptedServiceName, decryptedPublicKey), Times.Once);

        var serviceKeyFolder = Path.Combine(AppContext.BaseDirectory, "Key", decryptedServiceName);
        Assert.True(Directory.Exists(serviceKeyFolder));

        var publicKeyPath = Path.Combine(serviceKeyFolder, "public.key");
        Assert.True(File.Exists(publicKeyPath));
        var savedKey = await File.ReadAllTextAsync(publicKeyPath);
        Assert.Equal(decryptedPublicKey, savedKey);
    }

    [Fact]
    public async Task Consume_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        const string encryptedServiceName = "encryptedServiceName";
        const string decryptedServiceName = "TestService";
        const string encryptedPublicKey = "encryptedPublicKey";
        const string decryptedPublicKey = "decryptedPublicKey";

        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = encryptedPublicKey
        };

        _decryptInfoMock.Setup(d => d.Decrypt(encryptedServiceName)).Returns(decryptedServiceName);
        _decryptInfoMock.Setup(d => d.Decrypt(encryptedPublicKey)).Returns(decryptedPublicKey);

        var contextMock = new Mock<ConsumeContext<PublicKeyMessage>>();
        contextMock.Setup(c => c.Message).Returns(message);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        var serviceKeyFolder = Path.Combine(AppContext.BaseDirectory, "Key", decryptedServiceName);
        Assert.True(Directory.Exists(serviceKeyFolder));
    }

    [Fact]
    public async Task Consume_ShouldNotThrowException_WhenDirectoryCreationFails()
    {
        // Arrange
        const string encryptedServiceName = "encryptedServiceName";
        const string decryptedServiceName = "FailingService";
        const string encryptedPublicKey = "encryptedPublicKey";
        const string decryptedPublicKey = "decryptedPublicKey";

        var message = new PublicKeyMessage
        {
            ServiceName = encryptedServiceName,
            PublicKey = encryptedPublicKey
        };

        _decryptInfoMock.Setup(d => d.Decrypt(encryptedServiceName)).Returns(decryptedServiceName);
        _decryptInfoMock.Setup(d => d.Decrypt(encryptedPublicKey)).Returns(decryptedPublicKey);

        var contextMock = new Mock<ConsumeContext<PublicKeyMessage>>();
        contextMock.Setup(c => c.Message).Returns(message);
    }
}