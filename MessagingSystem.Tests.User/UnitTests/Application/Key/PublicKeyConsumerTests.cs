using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.User.Application.Messaging.Key;
using MessagingSystem.Services.User.Infrastructure.Keys;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Application.Key;

public class PublicKeyConsumerTests
{
    private readonly Mock<IDecryptionInfo> _decryptMock = new();
    private readonly Mock<IPublicKeyStorage> _storageMock = new();
    private readonly Mock<ConsumeContext<PublicKeyMessage>> _contextMock = new();

    private void CleanDirectory(string serviceName)
    {
        var keyFolder = Path.Combine(AppContext.BaseDirectory, "Key", serviceName);
        if (Directory.Exists(keyFolder))
            Directory.Delete(keyFolder, true);
    }

    [Fact]
    public async Task Consume_Should_Not_Save_When_ServiceName_Is_User()
    {
        // Arrange
        const string encryptedServiceName = "encryptedUser";
        var message = new PublicKeyMessage { ServiceName = encryptedServiceName, PublicKey = "encryptedKey" };
        _contextMock.Setup(c => c.Message).Returns(message);
        _decryptMock.Setup(d => d.Decrypt(encryptedServiceName)).Returns("User");

        var consumer = new PublicKeyConsumer(_decryptMock.Object, _storageMock.Object);

        // Act
        await consumer.Consume(_contextMock.Object);

        // Assert
        _storageMock.Verify(s => s.Save(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Consume_Should_Decrypt_And_Save_PublicKey_When_Valid()
    {
        // Arrange
        const string serviceName = "Auth";
        CleanDirectory(serviceName);

        var message = new PublicKeyMessage { ServiceName = "encryptedService", PublicKey = "encryptedKey" };
        _contextMock.Setup(c => c.Message).Returns(message);
        _decryptMock.Setup(d => d.Decrypt("encryptedService")).Returns(serviceName);
        _decryptMock.Setup(d => d.Decrypt("encryptedKey")).Returns("DecryptedPublicKey");

        var consumer = new PublicKeyConsumer(_decryptMock.Object, _storageMock.Object);

        var keyPath = Path.Combine(AppContext.BaseDirectory, "Key", serviceName, "public.key");

        // Act
        await consumer.Consume(_contextMock.Object);

        // Assert
        _storageMock.Verify(s => s.Save(serviceName, "DecryptedPublicKey"), Times.Once);
        Assert.True(File.Exists(keyPath));
        var fileContent = await File.ReadAllTextAsync(keyPath);
        Assert.Equal("DecryptedPublicKey", fileContent);

        // Cleanup
        CleanDirectory(serviceName);
    }

    [Fact]
    public async Task Consume_Should_Create_Directory_If_Not_Exists()
    {
        // Arrange
        const string serviceName = "MyService";
        CleanDirectory(serviceName);

        var message = new PublicKeyMessage { ServiceName = "encService", PublicKey = "encKey" };
        _contextMock.Setup(c => c.Message).Returns(message);
        _decryptMock.Setup(d => d.Decrypt("encService")).Returns(serviceName);
        _decryptMock.Setup(d => d.Decrypt("encKey")).Returns("MyKey");

        var consumer = new PublicKeyConsumer(_decryptMock.Object, _storageMock.Object);
        var dirPath = Path.Combine(AppContext.BaseDirectory, "Key", serviceName);
        var filePath = Path.Combine(dirPath, "public.key");

        // Assert pre-condition
        Assert.False(Directory.Exists(dirPath));

        // Act
        await consumer.Consume(_contextMock.Object);

        // Assert
        Assert.True(Directory.Exists(dirPath));
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal("MyKey", content);

        // Cleanup
        CleanDirectory(serviceName);
    }

    [Fact]
    public async Task Consume_Should_Overwrite_Existing_PublicKey_File()
    {
        // Arrange
        const string serviceName = "OverwriteService";
        var dirPath = Path.Combine(AppContext.BaseDirectory, "Key", serviceName);
        var filePath = Path.Combine(dirPath, "public.key");

        CleanDirectory(serviceName);
        Directory.CreateDirectory(dirPath);
        await File.WriteAllTextAsync(filePath, "OldKey");

        var message = new PublicKeyMessage { ServiceName = "encryptedService", PublicKey = "encryptedNewKey" };
        _contextMock.Setup(c => c.Message).Returns(message);
        _decryptMock.Setup(d => d.Decrypt("encryptedService")).Returns(serviceName);
        _decryptMock.Setup(d => d.Decrypt("encryptedNewKey")).Returns("NewKey");

        var consumer = new PublicKeyConsumer(_decryptMock.Object, _storageMock.Object);

        // Act
        await consumer.Consume(_contextMock.Object);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal("NewKey", content);

        // Cleanup
        CleanDirectory(serviceName);
    }
}
