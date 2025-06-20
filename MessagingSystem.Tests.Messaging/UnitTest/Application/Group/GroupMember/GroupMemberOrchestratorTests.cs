using Encryptor.Encryption;
using FluentAssertions;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using Moq;
using Xunit;
using Assert = Xunit.Assert;
using Theory = Xunit.TheoryAttribute;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupMember;

public class GroupMemberOrchestratorTests
{
    private readonly Mock<IGroupMembersRepository> _mockGroupMembersRepository;
    private readonly Mock<IHasher> _mockHasher;
    private readonly Mock<IEncryptionInfo> _mockEncryptionInfo;
    private readonly GroupMemberOrchestrator _orchestrator;

    public GroupMemberOrchestratorTests()
    {
        _mockGroupMembersRepository = new Mock<IGroupMembersRepository>();
        _mockHasher = new Mock<IHasher>();
        _mockEncryptionInfo = new Mock<IEncryptionInfo>();
        
        _orchestrator = new GroupMemberOrchestrator(
            _mockGroupMembersRepository.Object,
            _mockHasher.Object,
            _mockEncryptionInfo.Object
        );
    }

    #region UpdateMemberInfoAsync Tests
    
    [Fact]
    public async Task UpdateMemberInfoAsync_WithEmptyMembersList_ShouldReturnSuccessWithoutUpdating()
    {
        // Arrange
        var userHash = "non-existing-hash";
        var newNickName = "NewNickName";
        var image = "base64-image-data";
        var newHash = "new-hash";

        _mockGroupMembersRepository
            .Setup(x => x.FindUserByHashAsync(userHash))
            .ReturnsAsync(new List<GroupMembers>());

        _mockHasher
            .Setup(x => x.Hash(newNickName))
            .Returns(newHash);

        // Act
        var result = await _orchestrator.UpdateMemberInfoAsync(userHash, newNickName, image);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().Be("User info is updated");

        // Verify repository was called but no updates were made
        _mockGroupMembersRepository.Verify(x => x.FindUserByHashAsync(userHash), Times.Once);
        _mockHasher.Verify(x => x.Hash(newNickName), Times.Once);
        _mockEncryptionInfo.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Never);
        _mockGroupMembersRepository.Verify(x => x.EditUserInfoAsync(It.IsAny<GroupMembers>()), Times.Never);
    }
    
    [Theory]
    [InlineData("", "NewNickName", "image")]
    [InlineData("hash", "", "image")]
    [InlineData("hash", "NewNickName", "")]
    [InlineData(null, "NewNickName", "image")]
    [InlineData("hash", null, "image")]
    [InlineData("hash", "NewNickName", null)]
    public async Task UpdateMemberInfoAsync_WithNullOrEmptyParameters_ShouldStillProcess(
        string userHash, string newNickName, string image)
    {
        // Arrange
        var newHash = "new-hash";

        var member = new GroupMembers("OldNickName")
        {
            Id = Guid.NewGuid(),
            UserNickName = "OldNickName"
        };

        var members = new List<GroupMembers> { member };

        _mockGroupMembersRepository
            .Setup(x => x.FindUserByHashAsync(userHash))
            .ReturnsAsync(members);

        _mockHasher
            .Setup(x => x.Hash(newNickName))
            .Returns(newHash);

        _mockEncryptionInfo
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string input) => $"encrypted-{input}");

        _mockGroupMembersRepository
            .Setup(x => x.EditUserInfoAsync(It.IsAny<GroupMembers>()))
            .ReturnsAsync("Success");

        // Act
        var result = await _orchestrator.UpdateMemberInfoAsync(userHash, newNickName, image);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().Be("User info is updated");
    }


    [Fact]
    public async Task UpdateMemberInfoAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var userHash = "existing-hash";
        var newNickName = "NewNickName";
        var image = "base64-image-data";

        _mockGroupMembersRepository
            .Setup(x => x.FindUserByHashAsync(userHash))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _orchestrator.UpdateMemberInfoAsync(userHash, newNickName, image));
    }

    #endregion

    #region DeleteMemberInfoAsync Tests

    [Fact]
    public async Task DeleteMemberInfoAsync_WithValidHash_ShouldReturnSuccessWithRowCount()
    {
        // Arrange
        var hashNickName = "valid-hash";
        var affectedRows = 3;

        _mockGroupMembersRepository
            .Setup(x => x.DeleteUserInfoAsync(hashNickName))
            .ReturnsAsync(affectedRows);

        // Act
        var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().Be($"Delete successful. Rows affected: {affectedRows}");

        _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
    }

    [Fact]
    public async Task DeleteMemberInfoAsync_WithZeroRowsAffected_ShouldReturnSuccessWithZeroCount()
    {
        // Arrange
        var hashNickName = "valid-hash";
        var affectedRows = 0;

        _mockGroupMembersRepository
            .Setup(x => x.DeleteUserInfoAsync(hashNickName))
            .ReturnsAsync(affectedRows);

        // Act
        var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().Be("Delete successful. Rows affected: 0");

        _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
    }

    [Fact]
    public async Task DeleteMemberInfoAsync_WithNegativeRowsAffected_ShouldReturnFailure()
    {
        // Arrange
        var hashNickName = "invalid-hash";
        var affectedRows = -1;

        _mockGroupMembersRepository
            .Setup(x => x.DeleteUserInfoAsync(hashNickName))
            .ReturnsAsync(affectedRows);

        // Act
        var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("No rows were deleted. Possibly invalid user hash.");

        _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task DeleteMemberInfoAsync_WithNullOrEmptyHash_ShouldCallRepositoryAndHandleResult(string hashNickName)
    {
        // Arrange
        var affectedRows = 1;

        _mockGroupMembersRepository
            .Setup(x => x.DeleteUserInfoAsync(hashNickName))
            .ReturnsAsync(affectedRows);

        // Act
        var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().Be($"Delete successful. Rows affected: {affectedRows}");

        _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
    }

    [Fact]
    public async Task DeleteMemberInfoAsync_WhenRepositoryThrowsException_ShouldReturnFailureWithExceptionMessage()
    {
        // Arrange
        var hashNickName = "valid-hash";
        var exceptionMessage = "Database connection failed";
        var exception = new InvalidOperationException(exceptionMessage);

        _mockGroupMembersRepository
            .Setup(x => x.DeleteUserInfoAsync(hashNickName))
            .ThrowsAsync(exception);

        // Act
        var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be($"Exception occurred: {exceptionMessage}");

        _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
    }

    [Fact]
    public async Task DeleteMemberInfoAsync_WhenRepositoryThrowsExceptionWithInnerException_ShouldReturnFailureWithMainExceptionMessage()
    {
        // Arrange
        var hashNickName = "valid-hash";
        var innerException = new ArgumentException("Inner exception message");
        var mainException = new InvalidOperationException("Main exception message", innerException);

        _mockGroupMembersRepository
            .Setup(x => x.DeleteUserInfoAsync(hashNickName))
            .ThrowsAsync(mainException);

        // Act
        var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Exception occurred: Main exception message");

        _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
    }

    [Fact]
    public async Task DeleteMemberInfoAsync_WithLargePositiveRowCount_ShouldReturnSuccessWithCorrectCount()
    {
        // Arrange
        var hashNickName = "bulk-delete-hash";
        var affectedRows = 1000;

        _mockGroupMembersRepository
            .Setup(x => x.DeleteUserInfoAsync(hashNickName))
            .ReturnsAsync(affectedRows);

        // Act
        var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().Be($"Delete successful. Rows affected: {affectedRows}");

        _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
    }

    #endregion
}