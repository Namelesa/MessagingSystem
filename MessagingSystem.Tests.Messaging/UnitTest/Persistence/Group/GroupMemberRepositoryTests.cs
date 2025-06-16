using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Group;

public class GroupMemberRepositoryTests
{
    private Mock<IDbContextFactory<GroupAppDbContext>> CreateMockDbContextFactory(GroupAppDbContext context)
    {
        var mockFactory = new Mock<IDbContextFactory<GroupAppDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(context);
        return mockFactory;
    }

    private async Task<GroupAppDbContext> GetDbContextWithData()
    {
        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new GroupAppDbContext(options);
        
        var groupMember1 = new GroupMembers("Member");

        var groupMember2 = new GroupMembers("Admin");

        var groupInfo = new GroupInfo("group_1", "user_hash_1", "Group Name", "Group Description");

        groupInfo.AddUsers([groupMember1.UserNickName, groupMember2.UserNickName]);
        
        await context.GroupMembers.AddRangeAsync(groupMember1, groupMember2);
        await context.GroupInfos.AddAsync(groupInfo);
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task FindUserByHashAsync_WithValidHash_ReturnsUsers()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.FindUserByHashAsync("user_hash_1");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("user_hash_1", result.First().UserNickNameHash);
    }

    [Fact]
    public async Task FindUserByHashAsync_WithNonExistentHash_ReturnsEmptyList()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.FindUserByHashAsync("non_existent_hash");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindUserByHashAsync_WithNullHash_ReturnsEmptyList()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.FindUserByHashAsync(null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindUserByHashAsync_WithEmptyHash_ReturnsEmptyList()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.FindUserByHashAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindUserByHashAsync_WithWhitespaceHash_ReturnsEmptyList()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.FindUserByHashAsync("   ");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task EditUserInfoAsync_WithInvalidGroupMember_ReturnsExceptionString()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);
        
        var invalidMember = new GroupMembers("Member");

        // Act
        var result = await repository.EditUserInfoAsync(invalidMember);

        // Assert
        Assert.NotEqual("Edit is ok", result);
        Assert.Contains("Exception", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_WithValidUserHash_ReturnsAffectedRowsCount()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.DeleteUserInfoAsync("user_hash_1");

        // Assert
        Assert.True(result > 0);
        
        var remainingMembers = await context.GroupMembers
            .Where(m => m.UserNickNameHash == "user_hash_1")
            .ToListAsync();
        Assert.Empty(remainingMembers);

        var remainingGroups = await context.GroupInfos
            .Where(g => g.AdminHash == "user_hash_1")
            .ToListAsync();
        Assert.Empty(remainingGroups);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_WithNonExistentUserHash_ReturnsZero()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.DeleteUserInfoAsync("non_existent_hash");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_WithNullUserHash_ReturnsZero()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        // Act
        var result = await repository.DeleteUserInfoAsync(null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_RemovesFromBothTables()
    {
        // Arrange
        var context = await GetDbContextWithData();
        var mockFactory = CreateMockDbContextFactory(context);
        var repository = new GroupMemberRepository(mockFactory.Object);

        var userHash = "user_hash_1";
        
        var membersBefore = await context.GroupMembers
            .Where(m => m.UserNickNameHash == userHash)
            .CountAsync();
        var groupsBefore = await context.GroupInfos
            .Where(g => g.AdminHash == userHash)
            .CountAsync();

        Assert.True(membersBefore > 0);
        Assert.True(groupsBefore > 0);

        // Act
        var result = await repository.DeleteUserInfoAsync(userHash);

        // Assert
        Assert.Equal(membersBefore + groupsBefore, result);

        var membersAfter = await context.GroupMembers
            .Where(m => m.UserNickNameHash == userHash)
            .CountAsync();
        var groupsAfter = await context.GroupInfos
            .Where(g => g.AdminHash == userHash)
            .CountAsync();

        Assert.Equal(0, membersAfter);
        Assert.Equal(0, groupsAfter);
    }
}