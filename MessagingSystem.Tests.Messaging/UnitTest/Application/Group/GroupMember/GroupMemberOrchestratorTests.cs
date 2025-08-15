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
    public async Task DeleteMemberInfoAsync_WithValidHash_ShouldReturnSuccessWithGroupIds()
{
    // Arrange
    var hashNickName = "valid-hash";
    var affectedRows = 3;

    var members = new List<GroupMembers>
    {
        new GroupMembers("nick") { GroupId = Guid.NewGuid() },
        new GroupMembers("nick") { GroupId = Guid.NewGuid() }
    };

    _mockGroupMembersRepository
        .Setup(x => x.FindUserByHashAsync(hashNickName))
        .ReturnsAsync(members);

    _mockGroupMembersRepository
        .Setup(x => x.DeleteUserInfoAsync(hashNickName))
        .ReturnsAsync(affectedRows);

    // Act
    var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.Data.Should().BeEquivalentTo(members.Select(m => m.GroupId).Distinct());

    _mockGroupMembersRepository.Verify(x => x.FindUserByHashAsync(hashNickName), Times.Once);
    _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
}

    [Fact]
    public async Task DeleteMemberInfoAsync_WithZeroRowsAffected_ShouldStillReturnGroupIds()
{
    // Arrange
    var hashNickName = "valid-hash";
    var affectedRows = 0;

    var members = new List<GroupMembers>
    {
        new GroupMembers("nick") { GroupId = Guid.NewGuid() }
    };

    _mockGroupMembersRepository
        .Setup(x => x.FindUserByHashAsync(hashNickName))
        .ReturnsAsync(members);

    _mockGroupMembersRepository
        .Setup(x => x.DeleteUserInfoAsync(hashNickName))
        .ReturnsAsync(affectedRows);

    // Act
    var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

    // Assert
    result.Success.Should().BeTrue();
    result.Data.Should().ContainSingle().Which.Should().Be(members[0].GroupId);

    _mockGroupMembersRepository.Verify(x => x.FindUserByHashAsync(hashNickName), Times.Once);
}

    [Fact]
    public async Task DeleteMemberInfoAsync_WithNegativeRowsAffected_ShouldReturnFailure()
{
    // Arrange
    var hashNickName = "invalid-hash";
    var affectedRows = -1;

    var members = new List<GroupMembers>
    {
        new GroupMembers("nick") { GroupId = Guid.NewGuid() }
    };

    _mockGroupMembersRepository
        .Setup(x => x.FindUserByHashAsync(hashNickName))
        .ReturnsAsync(members);

    _mockGroupMembersRepository
        .Setup(x => x.DeleteUserInfoAsync(hashNickName))
        .ReturnsAsync(affectedRows);

    // Act
    var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be("No rows were deleted. Possibly invalid user hash.");

    _mockGroupMembersRepository.Verify(x => x.FindUserByHashAsync(hashNickName), Times.Once);
    _mockGroupMembersRepository.Verify(x => x.DeleteUserInfoAsync(hashNickName), Times.Once);
}

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task DeleteMemberInfoAsync_WithNullOrEmptyHash_ShouldReturnSuccess(string hashNickName)
{
    // Arrange
    var affectedRows = 1;
    var members = new List<GroupMembers>
    {
        new GroupMembers("nick") { GroupId = Guid.NewGuid() }
    };

    _mockGroupMembersRepository
        .Setup(x => x.FindUserByHashAsync(hashNickName))
        .ReturnsAsync(members);

    _mockGroupMembersRepository
        .Setup(x => x.DeleteUserInfoAsync(hashNickName))
        .ReturnsAsync(affectedRows);

    // Act
    var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

    // Assert
    result.Success.Should().BeTrue();
    result.Data.Should().ContainSingle().Which.Should().Be(members[0].GroupId);

    _mockGroupMembersRepository.Verify(x => x.FindUserByHashAsync(hashNickName), Times.Once);
}

    [Fact]
    public async Task DeleteMemberInfoAsync_WhenRepositoryThrows_ShouldReturnFailure()
{
    // Arrange
    var hashNickName = "valid-hash";
    var exceptionMessage = "Database connection failed";

    _mockGroupMembersRepository
        .Setup(x => x.FindUserByHashAsync(hashNickName))
        .ThrowsAsync(new InvalidOperationException(exceptionMessage));

    // Act
    var result = await _orchestrator.DeleteMemberInfoAsync(hashNickName);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be($"Exception occurred: {exceptionMessage}");

    _mockGroupMembersRepository.Verify(x => x.FindUserByHashAsync(hashNickName), Times.Once);
}
    
    #endregion
}