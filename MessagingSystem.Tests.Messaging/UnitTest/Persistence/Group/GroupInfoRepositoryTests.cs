using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupInformation;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Group;

public class GroupInfoRepositoryTests
{
    private Guid _testGroupId;

    private async Task<IDbContextFactory<GroupAppDbContext>> GetDbContextFactoryWithData()
    {
        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);

        await using var context = await factory.CreateDbContextAsync();

        var groupInfo = new GroupInfo(
            groupName: "Test Group",
            image: null,
            description: "Test Description",
            admin: "admin"
        );
        groupInfo.SetHash("admin_hash", "group_hash");

        groupInfo.AddUser("user1");
        groupInfo.AddUser("user2");
        
        groupInfo.ApplyHashToMembers(nick => $"{nick}_hash");

        await context.GroupInfos.AddAsync(groupInfo);
        await context.SaveChangesAsync();
        
        _testGroupId = groupInfo.Id;

        return factory;
    }

    private IDbContextFactory<GroupAppDbContext> GetEmptyDbContextFactory()
    {
        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task FindGroupByIdAsync_ReturnsGroupWithMembers()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);
        
        var newGroup = new GroupInfo(
            groupName: "group",
            image: null,
            description: "New Description",
            admin: "new_admin"
        );
        newGroup.SetHash("new_admin_hash", "group_hash");
        
        var result = await repo.CreateGroupAsync(newGroup);

        var group = await repo.FindGroupByIdAsync(result.Id);

        Assert.NotNull(group);
        Assert.Equal(result.Id, group.Id);
        Assert.Equal("group_hash", group.GroupNameHash);
    }

    [Fact]
    public async Task FindGroupByIdAsync_ReturnsNull_WhenGroupNotFound()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);
        var nonExistentId = Guid.NewGuid();

        var group = await repo.FindGroupByIdAsync(nonExistentId);

        Assert.Null(group);
    }

    [Fact]
    public async Task FindGroupByNameHashAsync_ReturnsGroupWithMembers()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var group = await repo.FindGroupByNameHashAsync("group_hash");

        Assert.NotNull(group);
        Assert.Equal("group_hash", group.GroupNameHash);
        Assert.Equal(2, group.Members.Count);
    }

    [Fact]
    public async Task FindGroupByNameHashAsync_ReturnsNull_WhenGroupNotFound()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var group = await repo.FindGroupByNameHashAsync("nonexistent_hash");

        Assert.Null(group);
    }

    [Fact]
    public async Task FindGroupByAdminHashAsync_ReturnsGroupsList()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var groups = await repo.FindGroupByAdminHashAsync("admin_hash");

        Assert.NotNull(groups);
        Assert.Single(groups);
        Assert.Equal("admin_hash", groups.First().AdminHash);
    }

    [Fact]
    public async Task FindGroupByAdminHashAsync_ReturnsEmptyList_WhenNoGroupsFound()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var groups = await repo.FindGroupByAdminHashAsync("nonexistent_admin");

        Assert.NotNull(groups);
        Assert.Empty(groups);
    }

    [Fact]
    public async Task CreateGroupAsync_AddsGroupToDatabase()
    {
        var factory = GetEmptyDbContextFactory();
        var repo = new GroupInfoRepository(factory);

        var newGroup = new GroupInfo(
            groupName: "New Group",
            image: null,
            description: "New Description",
            admin: "new_admin"
        );
        newGroup.SetHash("new_admin_hash", "new_group_hash");

        var result = await repo.CreateGroupAsync(newGroup);

        Assert.NotNull(result);
        Assert.Equal("new_group_hash", result.GroupNameHash);
        
        var foundGroup = await repo.FindGroupByNameHashAsync("new_group_hash");
        Assert.NotNull(foundGroup);
    }

    [Fact]
    public async Task EditGroupInfoAsync_UpdatesGroup()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var group = await repo.FindGroupByNameHashAsync("group_hash");
        Assert.NotNull(group);
        
        group.EditInfo("Updated Group", null, "Updated Description", "updated_group_hash");

        var result = await repo.EditGroupInfoAsync(group);

        Assert.Equal("updated_group_hash", result.GroupNameHash);
        
        var updatedGroup = await repo.FindGroupByIdAsync(group.Id);
        Assert.Equal("updated_group_hash", updatedGroup?.GroupNameHash);
    }

    [Fact]
    public async Task DeleteGroupAsync_RemovesGroup()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var group = await repo.FindGroupByNameHashAsync("group_hash");
        Assert.NotNull(group);

        await repo.DeleteGroupAsync(group);

        var deletedGroup = await repo.FindGroupByIdAsync(_testGroupId);
        Assert.Null(deletedGroup);
    }

    [Fact]
    public async Task GetGroupsByUserAsync_ReturnsGroupsWhereUserIsMember()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var groups = await repo.GetGroupsByUserAsync("user1_hash");

        Assert.NotNull(groups);
        Assert.Single(groups);
        Assert.Equal("user1_hash", groups[0].Members.First().UserNickNameHash);
    }

    [Fact]
    public async Task GetGroupsByUserAsync_ReturnsGroupsWhereUserIsAdmin()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var groups = await repo.GetGroupsByUserAsync("admin_hash");

        Assert.NotNull(groups);
        Assert.Single(groups);
        Assert.Equal("admin_hash", groups.First().AdminHash);
    }

    [Fact]
    public async Task GetGroupsByUserAsync_ReturnsEmptyList_WhenUserNotFound()
    {
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);

        var groups = await repo.GetGroupsByUserAsync("nonexistent_user");

        Assert.NotNull(groups);
        Assert.Empty(groups);
    }

    [Fact]
    public async Task GetGroupsByUserAsync_ReturnsGroupsForUserWithHashedNickname()
    {
        // Arrange
        var factory = await GetDbContextFactoryWithData();
        var repo = new GroupInfoRepository(factory);
        
        await using var context = await factory.CreateDbContextAsync();
        var group = await context.GroupInfos.FirstAsync();
        group.AddUser("user3");
        group.ApplyHashToMembers(nick => $"{nick}_hash");
        await context.SaveChangesAsync();

        // Act
        var groups = await repo.GetGroupsByUserAsync("user3_hash");

        // Assert
        Assert.NotNull(groups);
        Assert.Single(groups);
        Assert.Contains(groups.First().Members, m => m.UserNickNameHash == "user3_hash");
    }
    
    private class TestDbContextFactory(DbContextOptions<GroupAppDbContext> options)
        : IDbContextFactory<GroupAppDbContext>
    {
        public GroupAppDbContext CreateDbContext()
        {
            return new GroupAppDbContext(options);
        }

        public Task<GroupAppDbContext> CreateDbContextAsync()
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}