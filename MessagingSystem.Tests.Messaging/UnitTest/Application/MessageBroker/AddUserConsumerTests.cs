using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Add;
using MessagingSystem.Services.Messaging.Application.MessageBroker.AddUser;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.MessageBroker;

public class AddUserConsumerTests
{
    private readonly Mock<IDecryptionInfo> _decryptionInfo = new();
    private readonly Mock<IUserImageRepository> _userImageRepository = new();
    private readonly Mock<IPublicKeyStorage> _publicKeyStorage = new();
    private readonly Mock<IEncryptionInfo> _encryptionInfo = new();
    private readonly AddUserConsumer _consumer;

    public AddUserConsumerTests()
    {
        _consumer = new AddUserConsumer(
            _decryptionInfo.Object,
            _userImageRepository.Object,
            _publicKeyStorage.Object,
            _encryptionInfo.Object);
    }

    [Fact]
    public async Task Consume_ShouldReturn_WhenPublicKeyIsNull()
    {
        // Arrange
        _publicKeyStorage.Setup(p => p.Get("User")).Returns((string?)null);

        var contextMock = new Mock<ConsumeContext<AddUserRequest>>();
        contextMock.SetupGet(c => c.Message).Returns(new AddUserRequest("nickHash", "imageData"));

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _userImageRepository.Verify(r => r.AddUserImageAsync(It.IsAny<UserImage>()), Times.Never);
        _encryptionInfo.Verify(e => e.EncryptObjectStrings(It.IsAny<object>()), Times.Never);
        _encryptionInfo.Verify(e => e.EncryptRsaObjectStrings(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        contextMock.Verify(c => c.RespondAsync(It.IsAny<AddUserResponse>(), default), Times.Never);
    }

    [Fact]
    public async Task Consume_ShouldProcessAndRespond_WhenPublicKeyIsPresent()
    {
        // Arrange
        var nickHash = "encryptedNick";
        var imageData = "encryptedImage";
        var decryptedNick = "decryptedNick";
        var decryptedImage = "decryptedImage";
        var publicKey = "publicKey";

        _publicKeyStorage.Setup(p => p.Get("User")).Returns(publicKey);
        _decryptionInfo.Setup(d => d.DecryptRsa(nickHash)).Returns(decryptedNick);
        _decryptionInfo.Setup(d => d.DecryptRsa(imageData)).Returns(decryptedImage);

        var contextMock = new Mock<ConsumeContext<AddUserRequest>>();
        contextMock.SetupGet(c => c.Message).Returns(new AddUserRequest(nickHash, imageData));

        AddUserResponse? responseSent = null;
        contextMock.Setup(c => c.RespondAsync(It.IsAny<AddUserResponse>()))
            .Callback<AddUserResponse>(resp => responseSent = resp)
            .Returns(Task.CompletedTask);

        _userImageRepository
            .Setup(r => r.AddUserImageAsync(It.IsAny<UserImage>()))
            .ReturnsAsync((UserImage u) => u);

        _encryptionInfo.Setup(e => e.EncryptObjectStrings(It.IsAny<object>())).Verifiable();
        _encryptionInfo.Setup(e => e.EncryptRsaObjectStrings(It.IsAny<object>(), publicKey)).Verifiable();

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _userImageRepository.Verify(r => r.AddUserImageAsync(It.Is<UserImage>(u =>
            u.NickNameHash == decryptedNick && u.Image == decryptedImage)), Times.Once);

        _encryptionInfo.Verify(e => e.EncryptObjectStrings(It.IsAny<object>()), Times.Once);
        _encryptionInfo.Verify(e => e.EncryptRsaObjectStrings(It.IsAny<object>(), publicKey), Times.Once);

        contextMock.Verify(c => c.RespondAsync(It.IsAny<AddUserResponse>()), Times.Once);

        Assert.NotNull(responseSent);
        Assert.True(responseSent!.Success);
    }


    [Fact]
    public async Task Consume_ShouldThrow_WhenAddUserImageAsyncFails()
    {
        // Arrange
        var nickHash = "nickHash";
        var imageData = "imageData";
        var decryptedNick = "nick";
        var decryptedImage = "image";
        var publicKey = "publicKey";

        _publicKeyStorage.Setup(p => p.Get("User")).Returns(publicKey);
        _decryptionInfo.Setup(d => d.DecryptRsa(nickHash)).Returns(decryptedNick);
        _decryptionInfo.Setup(d => d.DecryptRsa(imageData)).Returns(decryptedImage);

        var contextMock = new Mock<ConsumeContext<AddUserRequest>>();
        contextMock.SetupGet(c => c.Message).Returns(new AddUserRequest(nickHash, imageData));

        _userImageRepository.Setup(r => r.AddUserImageAsync(It.IsAny<UserImage>()))
            .ThrowsAsync(new Exception("DB failure"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _consumer.Consume(contextMock.Object));
    }
}