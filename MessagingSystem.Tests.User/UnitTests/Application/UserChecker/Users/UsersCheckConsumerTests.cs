using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Messaging.UserChecker;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Application.UserChecker.Users;

public class UsersCheckConsumerTests
{ 
    [Fact]
    public async Task Consume_IfPublicKeyNotFound_exits()
    {
        // Arrange
        var publicKeyStorage = new Mock<IPublicKeyStorage>();
        publicKeyStorage.Setup(s => s.Get("Messaging")).Returns((string?)null);
        var context = new Mock<ConsumeContext<ExistingUsersRequest>>();
        context.Setup(s => s.Message).Returns(new ExistingUsersRequest(["foo"]));

        var consumer = new UsersCheckerConsumer(
            Mock.Of<IDecryptionInfo>(),
            Mock.Of<IUserOrchestrator>(),
            publicKeyStorage.Object,
            Mock.Of<IEncryptionInfo>(),
            Mock.Of<IHasher>()
        );

        // Act
        await consumer.Consume(context.Object);

        // Assert
        publicKeyStorage.Verify(s => s.Get("Messaging"), Times.Once);
        context.Verify(s => s.RespondAsync(It.IsAny<ExistingUsersResponse>()), Times.Never);
    }

    [Fact]
    public async Task Consume_IfUserSuccess_responds()
    {
        // Arrange
        var publicKeyStorage = new Mock<IPublicKeyStorage>();
        publicKeyStorage.Setup(s => s.Get("Messaging")).Returns("public_key");

        var decryption = new Mock<IDecryptionInfo>();
        decryption.Setup(d => d.DecryptRsa("foo")).Returns("encryptedNick");

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

        var context = new Mock<ConsumeContext<ExistingUsersRequest>>();
        context.Setup(s => s.Message).Returns(new ExistingUsersRequest(["foo"]));

        // Act
        var consumer = new UsersCheckerConsumer(
            decryption.Object,
            orchestrator.Object,
            publicKeyStorage.Object,
            encryption.Object,
            hasher.Object
        );

        await consumer.Consume(context.Object);

        // Assert
        context.Verify(s => s.RespondAsync(It.Is<ExistingUsersResponse>(resp =>
            resp.Users.Count == 1 &&
            resp.Users[0].NickName == "UsernameRsa" &&
            resp.Users[0].Image == "ImageRsa" &&
            resp.Users[0].IsExist == true
        )), Times.Once);
    }

    [Fact]
    public async Task Consume_IfUserNotSuccess_responds()
    {
        // Arrange
        var publicKeyStorage = new Mock<IPublicKeyStorage>();
        publicKeyStorage.Setup(s => s.Get("Messaging")).Returns("public_key");

        var decryption = new Mock<IDecryptionInfo>();
        decryption.Setup(d => d.DecryptRsa("foo")).Returns("encryptedNick");

        decryption.Setup(d => d.Decrypt("encryptedNick")).Returns("decryptedNick");

        var hasher = new Mock<IHasher>();
        hasher.Setup(h => h.Hash("decryptedNick")).Returns("hashed");

        var orchestrator = new Mock<IUserOrchestrator>();

        orchestrator
            .Setup(o => o.FindUserByNickNameAsync("hashed"))
            .ReturnsAsync(OperationResult<UserFoundDto>.Fail("Not found"));

        var context = new Mock<ConsumeContext<ExistingUsersRequest>>();
        context.Setup(s => s.Message).Returns(new ExistingUsersRequest(["foo"]));

        // Act
        var consumer = new UsersCheckerConsumer(
            decryption.Object,
            orchestrator.Object,
            publicKeyStorage.Object,
            Mock.Of<IEncryptionInfo>(),
            hasher.Object
        );

        await consumer.Consume(context.Object);

        // Assert
        context.Verify(s => s.RespondAsync(It.Is<ExistingUsersResponse>(resp =>
            resp.Users.Count == 1 &&
            resp.Users[0].IsExist == false &&
            resp.Users[0].NickName == ""
        )), Times.Once);
    }

    [Fact]
    public async Task Consume_IfDecryptionThrows_responds()
    {
        // Arrange
        var publicKeyStorage = new Mock<IPublicKeyStorage>();
        publicKeyStorage.Setup(s => s.Get("Messaging")).Returns("public_key");

        var decryption = new Mock<IDecryptionInfo>();
        decryption.Setup(d => d.DecryptRsa("foo")).Throws(new Exception("decryption fails"));

        var context = new Mock<ConsumeContext<ExistingUsersRequest>>();
        context.Setup(s => s.Message).Returns(new ExistingUsersRequest(["foo"]));

        var consumer = new UsersCheckerConsumer(
            decryption.Object, 
            Mock.Of<IUserOrchestrator>(),
            publicKeyStorage.Object,
            Mock.Of<IEncryptionInfo>(),
            Mock.Of<IHasher>()
        );

        // Act
        await consumer.Consume(context.Object);

        // Assert
        context.Verify(s => s.RespondAsync(It.Is<ExistingUsersResponse>(resp =>
            resp.Users.Count == 1 &&
            resp.Users[0].IsExist == false &&
            resp.Users[0].NickName == ""
        )), Times.Once);
    }
}