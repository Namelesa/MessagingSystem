using System.Reflection;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.Group;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation;

public class GroupInfoOrchestratorTests
{
    private readonly Mock<IGroupInfoRepository> _groupInfoRepositoryMock;
    private readonly Mock<IGroupMembersRepository> _groupMembersRepositoryMock;
    private readonly Mock<IHasher> _hasherMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IValidator<GroupDto>> _validatorMock;
    private readonly Mock<IValidator<EditGroupDto>> _editValidatorMock;
    private readonly Mock<IGroupEncryption> _groupEncryptionMock;
    private readonly Mock<IUserOrchestrator> _userOrchestratorMock;
    private readonly GroupInfoOrchestrator _orchestrator;

    public GroupInfoOrchestratorTests()
    {
        _groupInfoRepositoryMock = new Mock<IGroupInfoRepository>();
        _groupMembersRepositoryMock = new Mock<IGroupMembersRepository>();
        _hasherMock = new Mock<IHasher>();
        _mapperMock = new Mock<IMapper>();
        _validatorMock = new Mock<IValidator<GroupDto>>();
        _editValidatorMock = new Mock<IValidator<EditGroupDto>>();
        _groupEncryptionMock = new Mock<IGroupEncryption>();
        Mock<ILogger<GroupInfoOrchestrator>> loggerMock = new();
        _userOrchestratorMock = new Mock<IUserOrchestrator>();

        _orchestrator = new GroupInfoOrchestrator(
            _groupInfoRepositoryMock.Object,
            _groupMembersRepositoryMock.Object,
            _hasherMock.Object,
            _mapperMock.Object,
            _validatorMock.Object,
            _editValidatorMock.Object,
            _groupEncryptionMock.Object,
            loggerMock.Object,
            _userOrchestratorMock.Object
        );
    }

    #region CreateGroupAsync Tests

    [Fact]
    public async Task CreateGroupAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", ["user1"],
            [1, 2, 3]);
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        var foundUsers = new List<FoundedUser>
        {
            new("user1", "user1-image.jpg"),
            new("admin", "user1-image.jpg")
        };

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GroupDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _hasherMock.Setup(h => h.Hash("admin")).Returns("admin-hash");
        _hasherMock.Setup(h => h.Hash("TestGroup")).Returns("group-hash");
        _hasherMock.Setup(h => h.Hash("user1")).Returns("user1-hash");
        _mapperMock.Setup(m => m.Map<GroupInfo>(It.IsAny<GroupDto>())).Returns(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(It.IsAny<GroupInfo>())).Returns(groupDto);
        _userOrchestratorMock.Setup(u => u.CheckUsersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Ok(foundUsers));
        _groupEncryptionMock.Setup(g => g.EncryptMembers(It.IsAny<string>())).Returns<string>(s => $"encrypted-{s}");

        // Act
        var result = await _orchestrator.CreateGroupAsync(groupDto);

        // Assert
        Assert.True(result.Success);
        _groupInfoRepositoryMock.Verify(r => r.CreateGroupAsync(It.IsAny<GroupInfo>()), Times.Once);
        _groupEncryptionMock.Verify(g => g.Encrypt(It.IsAny<GroupInfo>()), Times.Once);
        _groupEncryptionMock.Verify(g => g.DecryptGeneric(It.IsAny<GroupDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateGroupAsync_ValidationFails_ReturnsFailure()
    {
        // Arrange
        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", new List<string>(),
            new byte[] { 1, 2, 3 });
        var validationResult =
            new ValidationResult(new[] { new ValidationFailure("GroupName", "Group name is required") });

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GroupDto>(), default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _orchestrator.CreateGroupAsync(groupDto);

        // Assert
        Assert.False(result.Success);
        _groupInfoRepositoryMock.Verify(r => r.CreateGroupAsync(It.IsAny<GroupInfo>()), Times.Never);
    }

    [Fact]
    public async Task CreateGroupAsync_RepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", ["user1"],
            [1, 2, 3]);
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        var foundUsers = new List<FoundedUser>
        {
            new FoundedUser("admin", "user1-image.jpg"),
            new FoundedUser("user1", "user1-image.jpg")
        };

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GroupDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _hasherMock.Setup(h => h.Hash("admin")).Returns("admin-hash");
        _hasherMock.Setup(h => h.Hash("TestGroup")).Returns("group-hash");
        _mapperMock.Setup(m => m.Map<GroupInfo>(It.IsAny<GroupDto>())).Returns(groupInfo);
        _userOrchestratorMock.Setup(u => u.CheckUsersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Ok(foundUsers));
        _groupInfoRepositoryMock.Setup(r => r.CreateGroupAsync(It.IsAny<GroupInfo>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _orchestrator.CreateGroupAsync(groupDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cannot create group", result.Message);
    }

    [Fact]
    public async Task CreateGroupAsync_CheckUsersThrowsException_ThrowsInvalidOperationException()
    {
        // Arrange
        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", ["user1"],
            [1, 2, 3]);
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<GroupDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _hasherMock.Setup(h => h.Hash("admin")).Returns("admin-hash");
        _hasherMock.Setup(h => h.Hash("TestGroup")).Returns("group-hash");
        _mapperMock.Setup(m => m.Map<GroupInfo>(It.IsAny<GroupDto>())).Returns(groupInfo);
        _userOrchestratorMock.Setup(u => u.CheckUsersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Fail("Users not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _orchestrator.CreateGroupAsync(groupDto));
    }

    #endregion

    #region FindGroupByNameAsync Tests

    [Fact]
    public async Task FindGroupByNameAsync_GroupExists_ReturnsSuccess()
    {
        // Arrange
        var groupName = "TestGroup";
        var groupHash = "group-hash";
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", new List<string>(),
            new byte[] { 1, 2, 3 });

        _hasherMock.Setup(h => h.Hash(groupName)).Returns(groupHash);
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByNameHashAsync(groupHash)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);

        // Act
        var result = await _orchestrator.FindGroupByNameAsync(groupName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(groupDto, result.Data);
        _groupEncryptionMock.Verify(g => g.DecryptGeneric(groupDto), Times.Once);
    }

    [Fact]
    public async Task FindGroupByNameAsync_GroupNotExists_ReturnsFailure()
    {
        // Arrange
        var groupName = "NonExistentGroup";
        var groupHash = "group-hash";

        _hasherMock.Setup(h => h.Hash(groupName)).Returns(groupHash);
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByNameHashAsync(groupHash)).ReturnsAsync((GroupInfo)null);

        // Act
        var result = await _orchestrator.FindGroupByNameAsync(groupName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group not found", result.Message);
    }

    #endregion

    #region FindGroupByIdAsync Tests

    [Fact]
    public async Task FindGroupByIdAsync_GroupExists_ReturnsSuccessWithMembers()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        var user1 = new GroupMembers("user1");
        user1.SetImage("encrypted-image1");
        var user2 = new GroupMembers("user2");
        user2.SetImage("encrypted-image2");
        groupInfo.Members.Add(user1);
        groupInfo.Members.Add(user2);

        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", new List<string>(),
            new byte[] { 1, 2, 3 });

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);
        _groupEncryptionMock.Setup(g => g.DecryptMembers("encrypted-image1")).Returns("decrypted-image1");
        _groupEncryptionMock.Setup(g => g.DecryptMembers("encrypted-image2")).Returns("decrypted-image2");

        // Act
        var result = await _orchestrator.FindGroupByIdAsync(groupId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data?.Members);
        Assert.Equal(2, result.Data.Members.Count);
        Assert.Equal("decrypted-image1", result.Data.Members[0].Image);
        Assert.Equal("decrypted-image2", result.Data.Members[1].Image);
    }

    [Fact]
    public async Task FindGroupByIdAsync_GroupNotExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync((GroupInfo)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _orchestrator.FindGroupByIdAsync(groupId));
    }

    [Fact]
    public async Task FindGroupByIdAsync_DecryptionFails_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns((GroupDto)null);

        // Act
        var result = await _orchestrator.FindGroupByIdAsync(groupId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to decrypt group information", result.Message);
    }

    [Fact]
    public async Task FindGroupByIdAsync_MemberWithEmptyImage_SetsImageToNull()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        groupInfo.Members.Add(new GroupMembers("user1"));

        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", new List<string>(),
            new byte[] { 1, 2, 3 });

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);

        // Act
        var result = await _orchestrator.FindGroupByIdAsync(groupId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data?.Members);
        Assert.Single(result.Data.Members);
        Assert.Null(result.Data.Members[0].Image);
    }

    #endregion

    #region EditGroupInfoAsync Tests

    [Fact]
    public async Task EditGroupInfoAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var editGroupDto = new EditGroupDto("NewGroupName", "new-image.jpg", "New Description");
        var groupInfo = new GroupInfo("OldGroupName", "old-image.jpg", "Old Description", "admin");
        var groupDto = new GroupDto("NewGroupName", "new-image.jpg", "New Description", "admin", new List<string>(),
            new byte[] { 1, 2, 3 });

        _editValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<EditGroupDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _hasherMock.Setup(h => h.Hash("NewGroupName")).Returns("new-group-hash");
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo)).ReturnsAsync(groupInfo);

        // Act
        var result = await _orchestrator.EditGroupInfoAsync(groupId, editGroupDto);

        // Assert
        Assert.True(result.Success);
        _groupEncryptionMock.Verify(g => g.Decrypt(groupInfo), Times.Once);
        _groupEncryptionMock.Verify(g => g.Encrypt(groupInfo), Times.Once);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(groupInfo), Times.Once);
    }

    [Fact]
    public async Task EditGroupInfoAsync_ValidationFails_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var editGroupDto = new EditGroupDto("", null, "");
        var validationResult =
            new ValidationResult(new[] { new ValidationFailure("GroupName", "Group name is required") });

        _editValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<EditGroupDto>(), default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _orchestrator.EditGroupInfoAsync(groupId, editGroupDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Group name is required", result.Message);
    }

    [Fact]
    public async Task EditGroupInfoAsync_RepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var editGroupDto = new EditGroupDto("NewGroupName", "new-image.jpg", "New Description");
        var groupInfo = new GroupInfo("OldGroupName", "old-image.jpg", "Old Description", "admin");

        _editValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<EditGroupDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _hasherMock.Setup(h => h.Hash("NewGroupName")).Returns("new-group-hash");
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _orchestrator.EditGroupInfoAsync(groupId, editGroupDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cannot update group", result.Message);
    }

    [Fact]
    public async Task EditGroupInfoAsync_ImageIsNull_UsesGroupNameAsImage()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var editGroupDto = new EditGroupDto("NewGroupName", null, "New Description");
        var groupInfo = new GroupInfo("OldGroupName", "old-image.jpg", "Old Description", "admin");
        var groupDto = new GroupDto("NewGroupName", "NewGroupName", "New Description", "admin", new List<string>(),
            new byte[] { 1, 2, 3 });

        _editValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<EditGroupDto>(), default))
            .ReturnsAsync(new ValidationResult());
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _hasherMock.Setup(h => h.Hash("NewGroupName")).Returns("new-group-hash");
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo)).ReturnsAsync(groupInfo);

        // Act
        var result = await _orchestrator.EditGroupInfoAsync(groupId, editGroupDto);

        // Assert
        Assert.True(result.Success);
    }

    #endregion

    #region EditGroupsAdminAsync Tests

    [Fact]
    public async Task EditGroupsAdminAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var adminHash = "old-admin-hash";
        var newAdminNick = "newAdmin";
        var newAdminHash = "new-admin-hash";
        var groups = new List<GroupInfo>
        {
            new GroupInfo("Group1", "image1.jpg", "Description1", "oldAdmin"),
            new GroupInfo("Group2", "image2.jpg", "Description2", "oldAdmin")
        };

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByAdminHashAsync(adminHash)).ReturnsAsync(groups);
        _hasherMock.Setup(h => h.Hash(newAdminNick)).Returns(newAdminHash);
        _groupEncryptionMock.Setup(g => g.EncryptMembers(newAdminNick)).Returns("encrypted-newAdmin");

        // Act
        var result = await _orchestrator.EditGroupsAdminAsync(adminHash, newAdminNick);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Update is ok", result.Data);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(It.IsAny<GroupInfo>()), Times.Exactly(2));
    }

    #endregion

    #region DeleteGroupInfoAsync Tests

    [Fact]
    public async Task DeleteGroupInfoAsync_ValidAdmin_ReturnsSuccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminNickName = "admin";
        var adminHash = "admin-hash";
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);

        _hasherMock.Setup(h => h.Hash(adminNickName)).Returns(adminHash);
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);

        // Act
        var result = await _orchestrator.DeleteGroupInfoAsync(groupId, adminNickName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Group deleted successfully", result.Data);
        _groupInfoRepositoryMock.Verify(r => r.DeleteGroupAsync(groupInfo), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupInfoAsync_InvalidAdmin_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminNickName = "wrongAdmin";
        var adminHash = "wrong-admin-hash";
        var correctAdminHash = "correct-admin-hash";
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, correctAdminHash);

        _hasherMock.Setup(h => h.Hash(adminNickName)).Returns(adminHash);
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);

        // Act
        var result = await _orchestrator.DeleteGroupInfoAsync(groupId, adminNickName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You can't delete group", result.Message);
        _groupInfoRepositoryMock.Verify(r => r.DeleteGroupAsync(It.IsAny<GroupInfo>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroupInfoAsync_RepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminNickName = "admin";
        var adminHash = "admin-hash";
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);

        _hasherMock.Setup(h => h.Hash(adminNickName)).Returns(adminHash);
        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _groupInfoRepositoryMock.Setup(r => r.DeleteGroupAsync(groupInfo))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _orchestrator.DeleteGroupInfoAsync(groupId, adminNickName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cannot delete group", result.Message);
    }

    #endregion

    #region AddMembersToGroupAsync Tests

    [Fact]
    public async Task AddMembersToGroupAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";
        var dto = new GroupMembersDto { Users = ["user1", "user2"] };
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);
        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", new List<string>(),
            new byte[] { 1, 2, 3 });
        var foundUsers = new List<FoundedUser>
        {
            new FoundedUser("user1", "user1-image.jpg"),
            new FoundedUser("user2", "user2-image.jpg")
        };

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _userOrchestratorMock.Setup(u => u.CheckUsersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Ok(foundUsers));
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);
        _groupEncryptionMock.Setup(g => g.EncryptMembers(It.IsAny<string>())).Returns<string>(s => $"encrypted-{s}");

        // Act
        var result = await _orchestrator.AddMembersToGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.True(result.Success);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(groupInfo), Times.Once);
    }

    [Fact]
    public async Task AddMembersToGroupAsync_InvalidAdmin_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "wrong-admin-hash";
        var correctAdminHash = "correct-admin-hash";
        var dto = new GroupMembersDto { Users = ["user1"] };
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, correctAdminHash);

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);

        // Act
        var result = await _orchestrator.AddMembersToGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You can't add or delete members", result.Message);
    }

    [Fact]
    public async Task AddMembersToGroupAsync_EmptyUserList_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";
        var dto = new GroupMembersDto { Users = new List<string>() };
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);

        // Act
        var result = await _orchestrator.AddMembersToGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User list is empty", result.Message);
    }

    [Fact]
    public async Task AddMembersToGroupAsync_ValidAddOperation_UpdatesGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";
        var dto = new GroupMembersDto { Users = ["user1"] };

        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _userOrchestratorMock.Setup(u => u.CheckUsersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Ok([new FoundedUser("user1", "img.jpg")]));
        _groupEncryptionMock.Setup(g => g.EncryptMembers(It.IsAny<string>()))
            .Returns<string>(s => $"encrypted-{s}");
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(new GroupDto(
            "TestGroup", "image.jpg", "Description", "admin", new List<string>(), new byte[] { 1, 2, 3 }));

        // Act
        var result = await _orchestrator.AddMembersToGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.True(result.Success);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(groupInfo), Times.Once);
    }

    
    #endregion

    #region DeleteMembersFromGroupAsync Tests

    [Fact]
    public async Task DeleteMembersFromGroupAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";
        var dto = new GroupMembersDto { Users = ["user1", "user2"] };
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);
        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", [],
            [1, 2, 3]);

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);

        // Act
        var result = await _orchestrator.DeleteMembersFromGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.True(result.Success);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(groupInfo), Times.Once);
    }

    #endregion

    #region GetGroupsForUserAsync Tests

    [Fact]
    public async Task GetGroupsForUserAsync_UserHasGroups_ReturnsSuccess()
    {
        // Arrange
        var userNick = "user1";
        var userHash = "user1-hash";
        var groups = new List<GroupInfo>
        {
            new GroupInfo("Group1", "image1.jpg", "Description1", "admin1"),
            new GroupInfo("Group2", "image2.jpg", "Description2", "admin2")
        };
        var groupDtos = new List<GroupDto>
        {
            new GroupDto("Group1", "image1.jpg", "Description1", "admin1", new List<string>(), new byte[] { 1, 2, 3 }),
            new GroupDto("Group2", "image2.jpg", "Description2", "admin2", new List<string>(), new byte[] { 1, 2, 3 })
        };

        _hasherMock.Setup(h => h.Hash(userNick)).Returns(userHash);
        _groupInfoRepositoryMock.Setup(r => r.GetGroupsByUserAsync(userHash)).ReturnsAsync(groups);
        _mapperMock.Setup(m => m.Map<GroupDto>(groups[0])).Returns(groupDtos[0]);
        _mapperMock.Setup(m => m.Map<GroupDto>(groups[1])).Returns(groupDtos[1]);

        // Act
        var result = await _orchestrator.GetGroupsForUserAsync(userNick);

        // Assert
        Assert.True(result.Success);
        if (result.Data != null)
        {
            Assert.Equal(2, result.Data.Count);
            Assert.Contains(result.Data, g => g.GroupName == "Group1");
            Assert.Contains(result.Data, g => g.GroupName == "Group2");
        }
    }
    
    [Fact]
    public async Task GetGroupsForUserAsync_UserHasNoGroups_ReturnsEmptyList()
    {
        // Arrange
        var userNick = "user3";
        var userHash = "user3-hash";

        _hasherMock.Setup(h => h.Hash(userNick)).Returns(userHash);
        _groupInfoRepositoryMock.Setup(r => r.GetGroupsByUserAsync(userHash)).ReturnsAsync(new List<GroupInfo>());

        // Act
        var result = await _orchestrator.GetGroupsForUserAsync(userNick);

        // Assert
        Assert.False(result.Success);
        if (result.Message != null) Assert.NotEmpty(result.Message);
    }
    
    [Fact]
    public async Task DeleteMembersFromGroupAsync_UsersNotFoundInGroup_DoesNothingButReturnsSuccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";
        var dto = new GroupMembersDto { Users = ["userX"] };
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");

        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo)).ReturnsAsync(groupInfo);
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(new GroupDto("TestGroup", "image.jpg", "Description", "admin", new List<string>(), new byte[] { 1 }));

        // Act
        var result = await _orchestrator.DeleteMembersFromGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.True(result.Success);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(groupInfo), Times.Once);
    }

    
    #endregion
    
    [Fact]
    public async Task CheckUsersOrThrowAsync_InvalidUsers_ThrowsInvalidOperationException_Reflection()
    {
        // Arrange
        var users = new List<string> { "ghost1", "ghost2" };
        _userOrchestratorMock.Setup(u => u.CheckUsersAsync(users))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Ok(null)); 

        var method = typeof(GroupInfoOrchestrator)
            .GetMethod("CheckUsersOrThrowAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert
        var task = (Task)method?.Invoke(_orchestrator, [users])!;

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
    }
    
    [Fact]
    public async Task CheckUsersOrThrowAsync_SomeUsersMissing_ReturnsFailure()
    {
        var users = new List<string> { "user1", "ghost" };

        var found = new List<FoundedUser>
        {
            new FoundedUser("user1", "img1")
        };

        _userOrchestratorMock.Setup(x => x.CheckUsersAsync(users))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Ok(found));

        var method = typeof(GroupInfoOrchestrator)
            .GetMethod("CheckUsersOrThrowAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        var resultTask = (Task)method?.Invoke(_orchestrator, [users])!;
        await resultTask;

        var resultProperty = resultTask.GetType().GetProperty("Result");
        var result = (OperationResult<List<FoundedUser>>)resultProperty?.GetValue(resultTask)!;

        Assert.False(result.Success);
        Assert.Contains("ghost", result.Message);
    }

    [Fact]
    public async Task ModifyGroupMembersAsync_InvalidModificationType_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";

        var groupInfo = new GroupInfo("Group", "img", "desc", "admin");
        groupInfo.SetAdminHash(adminHash);

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId))
            .ReturnsAsync(groupInfo);

        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo))
            .ReturnsAsync(groupInfo);

        _groupEncryptionMock.Setup(g => g.DecryptMembers(It.IsAny<string>())).Returns("decrypted");

        var method = typeof(GroupInfoOrchestrator)
            .GetMethod("ModifyGroupMembersAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        var args = new object[] { groupId, adminHash, new List<string> { "u" }, (GroupMemberModificationType)99 };

        // Act
        var task = (Task)method.Invoke(_orchestrator, args);
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = (OperationResult<GroupDto>)resultProperty.GetValue(task);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Can not modify user members in group", result.Message);
    }


    
    [Fact]
    public async Task CheckUsersOrThrowAsync_NullData_ThrowsInvalidOperationException()
    {
        var users = new List<string> { "user1" };

        _userOrchestratorMock.Setup(x => x.CheckUsersAsync(users))
            .ReturnsAsync(OperationResult<List<FoundedUser>>.Ok(null));

        var method = typeof(GroupInfoOrchestrator)
            .GetMethod("CheckUsersOrThrowAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        var task = (Task)method.Invoke(_orchestrator, [users]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task!);
    }
    
    [Fact]
    public async Task DeleteMembersFromGroupAsync_TryToRemoveAdmin_ReturnsFailure()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";
        var dto = new GroupMembersDto { Users = ["admin"] }; 
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _hasherMock.Setup(h => h.Hash("admin")).Returns(adminHash); 

        // Act
        var result = await _orchestrator.DeleteMembersFromGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You can't remove yourself from group", result.Message);
        _groupMembersRepositoryMock.Verify(r => r.DeleteUsersByHashesAsync(It.IsAny<List<string>>()), Times.Never);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(It.IsAny<GroupInfo>()), Times.Never);
    }
    
    [Fact]
    public async Task DeleteMembersFromGroupAsync_ValidUsers_RemovesUsersSuccessfully()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin-hash";
        var dto = new GroupMembersDto { Users = ["user1", "user2"] };
        var groupInfo = new GroupInfo("TestGroup", "image.jpg", "Description", "admin");
        typeof(GroupInfo).GetProperty("AdminHash")?.SetValue(groupInfo, adminHash);

        var groupDto = new GroupDto("TestGroup", "image.jpg", "Description", "admin", [], [1, 2, 3]);

        groupInfo.Members.Add(new GroupMembers("user1"));
        groupInfo.Members.Add(new GroupMembers("user2"));
        groupInfo.Members.Add(new GroupMembers("admin"));

        _groupInfoRepositoryMock.Setup(r => r.FindGroupByIdAsync(groupId)).ReturnsAsync(groupInfo);
        _hasherMock.Setup(h => h.Hash("user1")).Returns("hash1");
        _hasherMock.Setup(h => h.Hash("user2")).Returns("hash2");
        _mapperMock.Setup(m => m.Map<GroupDto>(groupInfo)).Returns(groupDto);
        _groupInfoRepositoryMock.Setup(r => r.EditGroupInfoAsync(groupInfo)).ReturnsAsync(groupInfo);

        // Act
        var result = await _orchestrator.DeleteMembersFromGroupAsync(groupId, dto, adminHash);

        // Assert
        Assert.True(result.Success);
        _groupMembersRepositoryMock.Verify(r => r.DeleteUsersByHashesAsync(
            It.Is<List<string>>(l => l.Contains("hash1") && l.Contains("hash2"))), Times.Once);
        _groupInfoRepositoryMock.Verify(r => r.EditGroupInfoAsync(groupInfo), Times.Once);
    }
}