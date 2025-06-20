using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.User;

public class UserOrchestratorTests
{
    private readonly Mock<IEncryptionInfo> _encryptionInfoMock = new();
    private readonly Mock<IDecryptionInfo> _decryptionInfoMock = new();
    private readonly Mock<IRequestClient<ExistingUserRequest>> _clientMock = new();
    private readonly Mock<IRequestClient<ExistingUsersRequest>> _clientsMock = new();
    private readonly Mock<IPublicKeyStorage> _publicKeyStorageMock = new();
    private readonly Mock<IUserImageRepository> _userImageRepositoryMock = new();
    private readonly Mock<IHasher> _hasherMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private readonly UserOrchestrator _orchestrator;

    public UserOrchestratorTests()
    {
        _orchestrator = new UserOrchestrator(
            _encryptionInfoMock.Object,
            _decryptionInfoMock.Object,
            _clientMock.Object,
            _clientsMock.Object,
            _publicKeyStorageMock.Object,
            _userImageRepositoryMock.Object,
            _hasherMock.Object,
            _mapperMock.Object);
    }
    
    private UserOrchestrator CreateOrchestrator()
    {
        return new UserOrchestrator(
            _encryptionInfoMock.Object,
            _decryptionInfoMock.Object,
            _clientMock.Object,
            _clientsMock.Object,
            _publicKeyStorageMock.Object,
            _userImageRepositoryMock.Object,
            _hasherMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task CheckUserAsync_WhenPublicKeyNotFound_ShouldReturnFailResult()
    {
        // Arrange
        const string nickName = "testUser";
        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns((string?)null);

        // Act
        var result = await _orchestrator.CheckUserAsync(nickName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Public key for User service not found", result.Message);
    }

    [Fact]
    public async Task CheckUserAsync_WhenUserNotExist_ShouldReturnFailResult()
    {
        // Arrange
        const string nickName = "testUser";
        const string publicKey = "publicKey";
        const string encryptedNick = "encryptedNick";
        const string rsaEncryptedNick = "rsaEncryptedNick";

        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(publicKey);
        _encryptionInfoMock.Setup(x => x.Encrypt(nickName)).Returns(encryptedNick);
        _encryptionInfoMock.Setup(x => x.EncryptRsa(encryptedNick, publicKey)).Returns(rsaEncryptedNick);

        var responseMock = new Mock<Response<ExistingUserResponse>>();
        responseMock.Setup(x => x.Message).Returns(new ExistingUserResponse("", false, ""));
        _clientMock.Setup(x => x.GetResponse<ExistingUserResponse>(It.IsAny<ExistingUserRequest>(), default, default))
            .ReturnsAsync(responseMock.Object);

        // Act
        var result = await _orchestrator.CheckUserAsync(nickName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    [Fact]
    public async Task CheckUserAsync_WhenUserExistsAndSaveSuccessful_ShouldReturnSuccessResult()
    {
        // Arrange
        const string nickName = "testUser";
        const string publicKey = "publicKey";
        const string encryptedNick = "encryptedNick";
        const string rsaEncryptedNick = "rsaEncryptedNick";
        const string decryptedNick = "decryptedNick";
        const string decryptedImage = "decryptedImage";
        const string hashedNick = "hashedNick";

        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(publicKey);
        _encryptionInfoMock.Setup(x => x.Encrypt(nickName)).Returns(encryptedNick);
        _encryptionInfoMock.Setup(x => x.EncryptRsa(encryptedNick, publicKey)).Returns(rsaEncryptedNick);
        _decryptionInfoMock.Setup(x => x.DecryptRsa(It.IsAny<string>())).Returns("decryptedRsa");
        _decryptionInfoMock.Setup(x => x.Decrypt("decryptedRsa")).Returns(decryptedNick);
        _decryptionInfoMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns(decryptedImage);
        _hasherMock.Setup(x => x.Hash(decryptedNick)).Returns(hashedNick);

        var responseMock = new Mock<Response<ExistingUserResponse>>();
        responseMock.Setup(x => x.Message).Returns(new ExistingUserResponse("encryptedNick", true, "encryptedImage"));
        _clientMock.Setup(x => x.GetResponse<ExistingUserResponse>(It.IsAny<ExistingUserRequest>(), default, default))
            .ReturnsAsync(responseMock.Object);

        _mapperMock.Setup(x => x.Map<UserImage>(It.IsAny<FoundedUser>())).Returns(new UserImage("nick", "image1"));
        _userImageRepositoryMock.Setup(x => x.AddUserImageAsync(It.IsAny<UserImage>()))
            .ReturnsAsync(new UserImage("nick", "image1"));

        // Act
        var result = await _orchestrator.CheckUserAsync(nickName);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        _userImageRepositoryMock.Verify(x => x.AddUserImageAsync(It.IsAny<UserImage>()), Times.Once);
    }

    [Fact]
    public async Task CheckUserAsync_WhenSaveFails_ShouldReturnFailResult()
    {
        // Arrange
        const string nickName = "testUser";
        const string publicKey = "publicKey";

        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(publicKey);
        _encryptionInfoMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns("encrypted");
        _encryptionInfoMock.Setup(x => x.EncryptRsa(It.IsAny<string>(), It.IsAny<string>())).Returns("rsaEncrypted");
        _decryptionInfoMock.Setup(x => x.DecryptRsa(It.IsAny<string>())).Returns("decryptedRsa");
        _decryptionInfoMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("decrypted");
        _hasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");

        var responseMock = new Mock<Response<ExistingUserResponse>>();
        responseMock.Setup(x => x.Message).Returns(new ExistingUserResponse("nick", true, "image1"));
        _clientMock.Setup(x => x.GetResponse<ExistingUserResponse>(It.IsAny<ExistingUserRequest>(), default, default))
            .ReturnsAsync(responseMock.Object);

        _mapperMock.Setup(x => x.Map<UserImage>(It.IsAny<FoundedUser>())).Returns(new UserImage("nick", "image1"));
        _userImageRepositoryMock.Setup(x => x.AddUserImageAsync(It.IsAny<UserImage>()))
            .ThrowsAsync(new Exception("Database Message"));

        // Act
        var result = await _orchestrator.CheckUserAsync(nickName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Error saving user image", result.Message);
    }

    [Fact]
    public async Task CheckUsersAsync_WhenPublicKeyNotFound_ShouldReturnFailResult()
    {
        // Arrange
        var nickNames = new List<string> { "user1", "user2" };
        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns((string?)null);

        // Act
        var result = await _orchestrator.CheckUsersAsync(nickNames);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Public key for User service not found", result.Message);
    }

    [Fact]
    public async Task CheckUsersAsync_WhenUsersExist_ShouldReturnSuccessResult()
    {
        // Arrange
        var nickNames = new List<string> { "user1", "user2" };
        const string publicKey = "publicKey";

        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(publicKey);
        _encryptionInfoMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns("encrypted");
        _encryptionInfoMock.Setup(x => x.EncryptRsa(It.IsAny<string>(), It.IsAny<string>())).Returns("rsaEncrypted");
        _decryptionInfoMock.Setup(x => x.DecryptRsa(It.IsAny<string>())).Returns("decryptedRsa");
        _decryptionInfoMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("decrypted");

        var users = new List<ExistingUserDto>
        {
            new("nick1", true, "image1"),
            new("nick2", true, "image2")
        };

        var responseMock = new Mock<Response<ExistingUsersResponse>>();
        responseMock.Setup(x => x.Message).Returns(new ExistingUsersResponse(users));
        _clientsMock.Setup(x => x.GetResponse<ExistingUsersResponse>(It.IsAny<ExistingUsersRequest>(), default, default))
            .ReturnsAsync(responseMock.Object);

        // Act
        var result = await _orchestrator.CheckUsersAsync(nickNames);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task CheckUsersAsync_WhenNoUsersFound_ShouldReturnFailResult()
    {
        // Arrange
        var nickNames = new List<string> { "user1", "user2" };
        const string publicKey = "publicKey";

        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(publicKey);
        _encryptionInfoMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns("encrypted");
        _encryptionInfoMock.Setup(x => x.EncryptRsa(It.IsAny<string>(), It.IsAny<string>())).Returns("rsaEncrypted");

        var users = new List<ExistingUserDto>
        {
            new("nick1", false, "image1"),
            new("nick2", false, "image2")
        };

        var responseMock = new Mock<Response<ExistingUsersResponse>>();
        responseMock.Setup(x => x.Message).Returns(new ExistingUsersResponse(users));
        _clientsMock.Setup(x => x.GetResponse<ExistingUsersResponse>(It.IsAny<ExistingUsersRequest>(), default, default))
            .ReturnsAsync(responseMock.Object);

        // Act
        var result = await _orchestrator.CheckUsersAsync(nickNames);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No users found", result.Message);
    }
    
    [Fact]
    public async Task UpdateUserAsync_WhenUserExists_ShouldReturnSuccessResult()
    {
        // Arrange
        const string nickName = "testUser";
        const string image = "newImage.jpg";
        var userImage = new UserImage(nickName, image);

        _userImageRepositoryMock.Setup(x => x.FindUserImageByHashAsync(nickName)).ReturnsAsync(userImage);
        _userImageRepositoryMock
            .Setup(x => x.EditUserImageAsync(userImage))
            .ReturnsAsync(userImage);

        // Act
        var result = await _orchestrator.UpdateUserAsync(nickName, image);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User updated successfully", result.Data);
        _userImageRepositoryMock.Verify(x => x.EditUserImageAsync(userImage), Times.Once);
    }
    
    [Fact]
    public async Task DeleteUserAsync_ResultIsOperationResultWithCorrectMessage()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var nickName = "testuser";
        var hashedNick = "hashednick";

        var userImage = new UserImage(hashedNick, "imageData");
        _userImageRepositoryMock
            .Setup(r => r.FindUserImageByHashAsync(nickName))
            .ReturnsAsync(userImage);

        _userImageRepositoryMock
            .Setup(r => r.DeleteUserImageAsync(userImage))
            .ReturnsAsync(1); 

        _hasherMock.Setup(h => h.Hash(nickName)).Returns(hashedNick);

        // Act
        var result = await orchestrator.DeleteUserAsync(nickName);

        // Assert
        Assert.IsType<OperationResult<string>>(result);
        Assert.True(result.Success);          
        Assert.Equal("User deleted successfully", result.Data); 
    }
    
    [Fact]
    public async Task DeleteUserAsync_UserNotFound_DeletesEmptyUserAndReturnsSuccess()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var nickName = "unknownUser";
        var hashedNick = "hashedUnknown";

        var userImage = new UserImage(hashedNick, "");
        
        _userImageRepositoryMock
            .Setup(r => r.FindUserImageByHashAsync(nickName))
            .ReturnsAsync((UserImage?)null);
        _userImageRepositoryMock
            .Setup(r => r.DeleteUserImageAsync(userImage))
            .ReturnsAsync(0);

        _hasherMock.Setup(h => h.Hash(nickName)).Returns(hashedNick);

        // Act
        var result = await orchestrator.DeleteUserAsync(nickName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User deleted successfully", result.Data);
        _userImageRepositoryMock.Verify(r => r.DeleteUserImageAsync(It.Is<UserImage>(u => u.NickNameHash == hashedNick && u.Image == "")), Times.Once);
    }
    
    [Fact]
    public async Task DeleteUserAsync_DeleteThrowsException_ReturnsFail()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var nickName = "testuser";
        var hashedNick = "hashednick";

        var userImage = new UserImage(hashedNick, "imageData");
        _userImageRepositoryMock
            .Setup(r => r.FindUserImageByHashAsync(nickName))
            .ReturnsAsync(userImage);
        _userImageRepositoryMock
            .Setup(r => r.DeleteUserImageAsync(userImage))
            .ThrowsAsync(new Exception("DB error"));

        _hasherMock.Setup(h => h.Hash(nickName)).Returns(hashedNick);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => orchestrator.DeleteUserAsync(nickName));
    }
    
    [Fact]
    public async Task DeleteUserAsync_VerifyDeleteUserImageAsyncCalledWithCorrectUser()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var nickName = "testuser";
        var hashedNick = "hashednick";

        var userImage = new UserImage(hashedNick, "imageData");
        _userImageRepositoryMock
            .Setup(r => r.FindUserImageByHashAsync(nickName))
            .ReturnsAsync(userImage);
        _userImageRepositoryMock
            .Setup(r => r.DeleteUserImageAsync(It.IsAny<UserImage>()))
            .Returns(Task.FromResult(1));

        _hasherMock.Setup(h => h.Hash(nickName)).Returns(hashedNick);

        // Act
        await orchestrator.DeleteUserAsync(nickName);

        // Assert
        _userImageRepositoryMock.Verify(r => r.DeleteUserImageAsync(It.Is<UserImage>(u => u.NickNameHash == hashedNick && u.Image == "imageData")), Times.Once);
    }
}