using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.User;

public class UserOrchestratorTests
{
    private readonly Mock<IDecryptionInfo> _decryptionInfoMock = new();
    private readonly Mock<IUserImageRepository> _userImageRepositoryMock = new();
    private readonly Mock<IHasher> _hasherMock = new();

    private readonly UserOrchestrator _orchestrator;

    public UserOrchestratorTests()
    {
        _orchestrator = new UserOrchestrator(
            _userImageRepositoryMock.Object,
            _hasherMock.Object,
            _decryptionInfoMock.Object);
    }
    
    private UserOrchestrator CreateOrchestrator()
    {
        return new UserOrchestrator(
            _userImageRepositoryMock.Object,
            _hasherMock.Object,
            _decryptionInfoMock.Object);
    }
    
    [Fact]
    public async Task CheckUserAsync_WhenUserExists_ShouldReturnSuccessResult()
    {
        // Arrange
        const string nickName = "testUser";
        const string hashedNick = "hashedNick";
        const string decryptedImage = "decryptedImage";
        
        _hasherMock.Setup(x => x.Hash(nickName)).Returns(hashedNick);
        
        var userImage = new UserImage(hashedNick, "encryptedImageString");
        _userImageRepositoryMock.Setup(x => x.FindUserImageByHashAsync(hashedNick))
            .ReturnsAsync(userImage);
        
        _decryptionInfoMock.Setup(x => x.Decrypt(userImage.Image)).Returns(decryptedImage);

        // Act
        var result = await _orchestrator.CheckUserAsync(nickName);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(nickName, result.Data.NickName);
        Assert.Equal(decryptedImage, result.Data.Image);
    }

    [Fact]
    public async Task CheckUserAsync_WhenSaveFails_ShouldReturnFailResult()
    {
        // Arrange
        const string nickName = "testUser";
        
        _decryptionInfoMock.Setup(x => x.DecryptRsa(It.IsAny<string>())).Returns("decryptedRsa");
        _decryptionInfoMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("decrypted");
        _hasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        
        // Act
        var result = await _orchestrator.CheckUserAsync(nickName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    [Fact]
    public async Task CheckUsersAsync_WhenPublicKeyNotFound_ShouldReturnFailResult()
    {
        // Arrange
        var nickNames = new List<string> { "user1", "user2" };

        // Act
        var result = await _orchestrator.CheckUsersAsync(nickNames);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No users found", result.Message);
    }
    
    [Fact]
    public async Task CheckUsersAsync_WhenEmptyList_ShouldReturnFailResult()
    {
        // Arrange
        var nickNames = new List<string>();

        // Act
        var result = await _orchestrator.CheckUsersAsync(nickNames);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No nicknames provided", result.Message);
    }   

    [Fact]
    public async Task CheckUsersAsync_WhenSomeUsersFound_ShouldReturnSuccessWithFoundUsers()
    {
        // Arrange
        var nickNames = new List<string> { "user1", "user2", "user3" };
        const string hashedUser1 = "hashedUser1";
        const string hashedUser2 = "hashedUser2";
        const string hashedUser3 = "hashedUser3";
        const string decryptedImage1 = "decryptedImage1";
        const string decryptedImage2 = "decryptedImage2";
    
        _hasherMock.Setup(x => x.Hash("user1")).Returns(hashedUser1);
        _hasherMock.Setup(x => x.Hash("user2")).Returns(hashedUser2);
        _hasherMock.Setup(x => x.Hash("user3")).Returns(hashedUser3);
    
        // user1 найден
        var userImage1 = new UserImage(hashedUser1, "encryptedImage1");
        _userImageRepositoryMock.Setup(x => x.FindUserImageByHashAsync(hashedUser1))
            .ReturnsAsync(userImage1);
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedImage1")).Returns(decryptedImage1);
    
        var userImage2 = new UserImage(hashedUser2, "encryptedImage2");
        _userImageRepositoryMock.Setup(x => x.FindUserImageByHashAsync(hashedUser2))
        .ReturnsAsync(userImage2);
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedImage2")).Returns(decryptedImage2);
    
        _userImageRepositoryMock.Setup(x => x.FindUserImageByHashAsync(hashedUser3))
            .ReturnsAsync((UserImage?)null);

        // Act
        var result = await _orchestrator.CheckUsersAsync(nickNames);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
    
        var foundUser1 = result.Data.FirstOrDefault(u => u.NickName == "user1");
        Assert.NotNull(foundUser1);
        Assert.Equal(decryptedImage1, foundUser1.Image);
    
        var foundUser2 = result.Data.FirstOrDefault(u => u.NickName == "user2");
        Assert.NotNull(foundUser2);
        Assert.Equal(decryptedImage2, foundUser2.Image);
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