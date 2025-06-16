using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Messaging.UserChecker;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Application.UserChecker.User;

public class UserCheckerConsumerTests
{
    [Fact]
    public async Task Consume_IfPublicKeyNotFound_exits_earliest()
    {
        // Arrange
        var publicKeyStorage = new Mock<IPublicKeyStorage>();
        publicKeyStorage.Setup(s => s.Get("Messaging")).Returns((string?)null);
        
        var consumer = new UserCheckerConsumer(
            Mock.Of<IDecryptionInfo>(),
            Mock.Of<IUserOrchestrator>(),
            publicKeyStorage.Object,
            Mock.Of<IEncryptionInfo>(),
            Mock.Of<IHasher>());

        var context = new Mock<ConsumeContext<ExistingUserRequest>>();
        context.Setup(s => s.Message).Returns(new ExistingUserRequest("foo"));

        // Act
        await consumer.Consume(context.Object);

        // Assert
        publicKeyStorage.Verify(s => s.Get("Messaging"), Times.Once);
    }
    
    [Fact]
    public async Task Consume_IfUserSuccess_responds_with_isExist_True()
    {
        // Arrange
    var publicKeyStorage = new Mock<IPublicKeyStorage>();
    publicKeyStorage.Setup(s => s.Get("Messaging")).Returns("public_key");

    var decryption = new Mock<IDecryptionInfo>();
    decryption.Setup(d => d.DecryptRsa(It.IsAny<string>())).Returns("encryptedNick");

    decryption.Setup(d => d.Decrypt("encryptedNick")).Returns("decryptedNick");

    var hasher = new Mock<IHasher>();
    hasher.Setup(h => h.Hash("decryptedNick")).Returns("hashed");

    var orchestrator = new Mock<IUserOrchestrator>();

    orchestrator
        .Setup(o => o.FindUserByNickNameAsync("hashed"))
        .ReturnsAsync(OperationResult<UserFoundDto>.Ok(new UserFoundDto("encryptedUsername", "encryptedImage")));

    decryption.Setup(d => d.Decrypt("encryptedUsername")).Returns("Username");

    decryption.Setup(d => d.Decrypt("encryptedImage")).Returns("Image");

    var encryption = new Mock<IEncryptionInfo>();

    encryption.Setup(e => e.EncryptRsa("Username", "public_key")).Returns("UsernameRsa");

    encryption.Setup(e => e.EncryptRsa("Image", "public_key")).Returns("ImageRsa");

    var context = new Mock<ConsumeContext<ExistingUserRequest>>();
    context.Setup(s => s.Message).Returns(new ExistingUserRequest("foo"));

    // Act
    var consumer = new UserCheckerConsumer(
        decryption.Object,
        orchestrator.Object,
        publicKeyStorage.Object,
        encryption.Object,
        hasher.Object
    );

    await consumer.Consume(context.Object);

    // Assert
    decryption.Verify(d => d.DecryptRsa("foo"), Times.Once);
    decryption.Verify(d => d.Decrypt("encryptedUsername"), Times.Once);
    decryption.Verify(d => d.Decrypt("encryptedImage"), Times.Once);
    encryption.Verify(e => e.EncryptRsa("Username", "public_key"), Times.Once);
    encryption.Verify(e => e.EncryptRsa("Image", "public_key"), Times.Once);
    context.Verify(s => s.RespondAsync(It.Is<ExistingUserResponse>(resp =>
        resp.NickName == "UsernameRsa" &&
        resp.Image == "ImageRsa" &&
        resp.IsExist == true
    )), Times.Once);
    }
    
    [Fact]
    public async Task Consume_IfUserNotSuccess_responds_without_respond()
    {
        // Arrange
        var publicKeyStorage = new Mock<IPublicKeyStorage>();
        publicKeyStorage.Setup(s => s.Get("Messaging")).Returns("public_key");

        var decryption = new Mock<IDecryptionInfo>();
        decryption.Setup(d => d.DecryptRsa(It.IsAny<string>())).Returns("encryptedNick");

        decryption.Setup(d => d.Decrypt("encryptedNick")).Returns("decryptedNick");

        var hasher = new Mock<IHasher>();
        hasher.Setup(h => h.Hash("decryptedNick")).Returns("hashed");

        var orchestrator = new Mock<IUserOrchestrator>();

        orchestrator
            .Setup(o => o.FindUserByNickNameAsync("hashed"))
            .ReturnsAsync(OperationResult<UserFoundDto>.Fail("Не найден"));

        var context = new Mock<ConsumeContext<ExistingUserRequest>>();
        context.Setup(s => s.Message).Returns(new ExistingUserRequest("foo"));

        // Act
        var consumer = new UserCheckerConsumer(
            decryption.Object,
            orchestrator.Object,
            publicKeyStorage.Object,
            Mock.Of<IEncryptionInfo>(),
            hasher.Object
        );

        await consumer.Consume(context.Object);

        // Assert
        decryption.Verify(d => d.DecryptRsa("foo"), Times.Once);
        orchestrator.Verify(o => o.FindUserByNickNameAsync("hashed"), Times.Once);
        context.Verify(s => s.RespondAsync(It.IsAny<ExistingUserResponse>()), Times.Never);
    }
}