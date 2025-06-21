using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Group;

public class GroupMemberRepositoryTests : IDisposable
{
    private readonly Mock<IDbContextFactory<GroupAppDbContext>> _dbFactoryMock;
    private readonly GroupMemberRepository _repository;
    private readonly List<SqliteConnection> _connections = [];
    private readonly SqliteConnection _connection;

    public GroupMemberRepositoryTests()
    {
        SQLitePCL.Batteries.Init();

        _connection = new SqliteConnection("DataSource=memory:");
        _connection.Open();

        _dbFactoryMock = new Mock<IDbContextFactory<GroupAppDbContext>>();
        _repository = new GroupMemberRepository(_dbFactoryMock.Object);
    }

    private GroupAppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new GroupAppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
    
    private async Task<GroupAppDbContext> GetEmptyDbContext()
    {
        SQLitePCL.Batteries.Init();
        var connection = new SqliteConnection("DataSource=:memory:");
        _connections.Add(connection);
        await connection.OpenAsync();
        
        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new GroupAppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        return context;
    }
    
    [Fact]
    public async Task FindUserByHashAsync_WithNonExistentHash_ReturnsEmptyList()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(context);

        // Act
        var result = await _repository.FindUserByHashAsync("non_existent_hash");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindUserByHashAsync_WithNullHash_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _repository.FindUserByHashAsync(null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify that factory was never called
        _dbFactoryMock.Verify(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindUserByHashAsync_WithEmptyHash_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _repository.FindUserByHashAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify that factory was never called
        _dbFactoryMock.Verify(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindUserByHashAsync_WithWhitespaceHash_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = await _repository.FindUserByHashAsync("   ");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // Verify that factory was never called
        _dbFactoryMock.Verify(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task EditUserInfoAsync_WithNonExistentMember_ReturnsExceptionString()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(context);
        
        var memberToUpdate = new GroupMembers("updated_user");
        var nonExistentId = Guid.NewGuid();
        
        var idProperty = typeof(GroupMembers).GetProperty("Id");
        idProperty?.SetValue(memberToUpdate, nonExistentId);
        
        context.Entry(memberToUpdate).State = EntityState.Modified;

        // Act
        var result = await _repository.EditUserInfoAsync(memberToUpdate);

        // Assert
        Assert.NotEqual("Edit is ok", result);
        Assert.Contains("Exception", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_WithValidUserHash_DeletesFromBothTables()
    {
        await using (var context = GetDbContextForMy())
        {
            await context.Database.EnsureCreatedAsync();

            await context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO GroupInfos (Id, GroupName, Description, Admin, AdminHash, GroupNameHash, RowVersion)
                VALUES (
                    '00000000-0000-0000-0000-000000000001',
                    'Test Group',
                    'Description',
                    'only_admin',
                    'user_hash_1',
                    'group_hash',
                    x'01020304'
                )");

            await context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO GroupMembers (Id, UserNickName, UserNickNameHash, JoinedTime, GroupId)
                VALUES (
                    '00000000-0000-0000-0000-000000000002',
                    'TestUser',
                    'user_hash_1',
                    '2023-10-01T00:00:00Z',
                    '00000000-0000-0000-0000-000000000001'
                )");

            await context.SaveChangesAsync();
        }
        
        _dbFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GetDbContext());

        // Act
        var result = await _repository.DeleteUserInfoAsync("user_hash_1");

        // Assert
        Assert.True(result >= 0);

        await using (var context = GetDbContextForMy())
        {
            var membersCount = await context.GroupMembers.CountAsync(m => m.UserNickNameHash == "user_hash_1");
            var groupsCount = await context.GroupInfos.CountAsync(g => g.AdminHash == "user_hash_1");

            Assert.Equal(0, membersCount);
            Assert.Equal(0, groupsCount);
        }
    }
    
    [Fact]
    public async Task DeleteUserInfoAsync_WithNonExistentUserHash_ReturnsZero()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        
        // Act
        var result = await _repository.DeleteUserInfoAsync("non_existent_hash");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_DeletesOnlyFromGroupMembers_ReturnsCorrectCount()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        
        await context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO GroupInfos (Id, GroupName, Description, Admin, AdminHash, GroupNameHash, RowVersion)
      VALUES (
          '00000000-0000-0000-0000-000000000001',
          'Test Group',
          'Description',
          'only_admin',
          'only_admin_hash',
          'group_hash',
          x'01020304'
      )"
        );

        await context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO GroupMembers (Id, UserNickName, UserNickNameHash, JoinedTime, GroupId)
      VALUES (
          '00000000-0000-0000-0000-000000000002',
          'TestUser',
          'TestUserHash',
          '2023-10-01T00:00:00Z',
          '00000000-0000-0000-0000-000000000001'
      )"
        );

        await context.SaveChangesAsync();

        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(context);

        // Act
        var result = await _repository.DeleteUserInfoAsync("TestUserHash");

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_DeletesOnlyFromGroupInfos_ReturnsCorrectCount()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();

        await context.Database.ExecuteSqlRawAsync
        (@"INSERT INTO GroupInfos (Id, GroupName, Description, Admin, AdminHash, GroupNameHash, RowVersion)
        VALUES (
            '00000000-0000-0000-0000-000000000001',
            'Test Group',
            'Description',
            'only_admin',
            'only_admin_hash',
            'group_hash',
            x'01020304'
            )
        ");
        
        await context.SaveChangesAsync();

        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        // Act
        var result = await _repository.DeleteUserInfoAsync("only_admin_hash");

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_WithNullUserHash_ReturnsZero()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(context);

        // Act
        var result = await _repository.DeleteUserInfoAsync(null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task DeleteUserInfoAsync_WithEmptyUserHash_ReturnsZero()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                     .ReturnsAsync(context);

        // Act
        var result = await _repository.DeleteUserInfoAsync("");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task WithContextAsync_CallsFactoryToCreateContext()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        var factoryCalled = false;
    
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => 
            {
                factoryCalled = true;
                return context;
            });

        // Act
        await _repository.FindUserByHashAsync("any_hash");

        // Assert
        Assert.True(factoryCalled, "Factory should have been called to create context");
        _dbFactoryMock.Verify(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task DeleteUsersByHashesAsync_WithValidHashes_CallsFactoryOnce()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        var hashesToDelete = new[] { "user_hash_1", "user_hash_2" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCastException>(async () => 
            await _repository.DeleteUsersByHashesAsync(hashesToDelete));

        _dbFactoryMock.Verify(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUsersByHashesAsync_WithEmptyCollection_ThrowsInvalidCastException()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        var emptyHashes = Array.Empty<string>();

        // Act & Assert 
        await Assert.ThrowsAsync<InvalidCastException>(async () => 
            await _repository.DeleteUsersByHashesAsync(emptyHashes));
    }

    [Fact]
    public async Task DeleteUsersByHashesAsync_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = await GetEmptyDbContext();
        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _repository.DeleteUsersByHashesAsync(null));
    }
    
    [Fact]
    public async Task DeleteUsersByHashesAsync_WithValidHashesInDatabase_ReturnsCorrectCount()
    {
        // Arrange
        var context = await GetEmptyDbContext();
        var groupId = Guid.NewGuid();
    
        await context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO GroupInfos (Id, GroupName, Description, Admin, AdminHash, GroupNameHash, RowVersion)
            VALUES ({groupId}, 'Test Group', 'Description', 'admin', 'admin_hash', 'group_hash', x'01020304')");
        
        await context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO GroupMembers (Id, UserNickName, UserNickNameHash, JoinedTime, GroupId)
            VALUES ('00000000-0000-0000-0000-000000000001', 'TestUser1', 'hash_to_delete_1', '2023-10-01T00:00:00Z', {groupId})");

        await context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO GroupMembers (Id, UserNickName, UserNickNameHash, JoinedTime, GroupId)
            VALUES ('00000000-0000-0000-0000-000000000002', 'TestUser2', 'hash_to_delete_2', '2023-10-01T00:00:00Z', {groupId})");

        await context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO GroupMembers (Id, UserNickName, UserNickNameHash, JoinedTime, GroupId)
            VALUES ('00000000-0000-0000-0000-000000000003', 'TestUser3', 'hash_to_keep', '2023-10-01T00:00:00Z', {groupId})");

        await context.SaveChangesAsync();

        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(context);

        // Act
        var hashes = new[] { "hash_to_delete_1", "hash_to_delete_2" };
        var inClause = string.Join(", ", hashes.Select(h => $"'{h}'"));

        var sql = $@"
        DELETE FROM ""GroupMembers""
        WHERE ""UserNickNameHash"" IN ({inClause})
        ";

        var deletedCount = await context.Database.ExecuteSqlRawAsync(sql);

        // Assert
        Assert.Equal(2, deletedCount);

        var remainingCount = await context.GroupMembers.CountAsync();
        Assert.Equal(1, remainingCount);

        var remaining = await context.GroupMembers.FirstAsync();
        Assert.Equal("hash_to_keep", remaining.UserNickNameHash);
    }
    
    [Fact]
    public async Task EditUserInfoAsync_WithDatabaseError_ReturnsExceptionMessage()
    {
        // Arrange
        var context = GetDbContextForMy();
        await context.Database.EnsureCreatedAsync();

        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(context);
        
        var invalidMember = new GroupMembers("TestUser");
        var nonExistentId = Guid.NewGuid();
        var idProperty = typeof(GroupMembers).GetProperty("Id");
        idProperty?.SetValue(invalidMember, nonExistentId);

        // Act
        var result = await _repository.EditUserInfoAsync(invalidMember);

        // Assert
        Assert.NotEqual("Edit is ok", result);
        Assert.Contains("Exception", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteUsersByHashesAsync_WithSingleValidHash_ReturnsOne()
    {
        // Arrange
        var context = await GetEmptyDbContext();
        var groupId = Guid.NewGuid();

        await context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO GroupInfos (Id, GroupName, Description, Admin, AdminHash, GroupNameHash, RowVersion)
            VALUES (
                '{0}',
                'Test Group',
                'Description',
                'admin',
                'admin_hash',
                'group_hash',
                x'01020304'
            )", groupId);

        await context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO GroupMembers (Id, UserNickName, UserNickNameHash, JoinedTime, GroupId)
            VALUES (
                '00000000-0000-0000-0000-000000000001',
                'TestUser',
                'single_hash_to_delete',
                '2023-10-01T00:00:00Z',
                '{0}'
            )", groupId);

        await context.SaveChangesAsync();

        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(context);

        // Act
        var hashes = new[] { "single_hash_to_delete" };
        var inClause = string.Join(", ", hashes.Select(h => $"'{h}'"));

        var sql = $@"
        DELETE FROM ""GroupMembers""
        WHERE ""UserNickNameHash"" IN ({inClause})
        ";

        var deletedCount = await context.Database.ExecuteSqlRawAsync(sql);
    
        // Assert
        Assert.Equal(1, deletedCount);
    }
    
    [Fact]
    public async Task EditUserInfoAsync_WithValidMember_ReturnsSuccessMessage()
    {
        // Arrange
        var context = GetDbContextForMy();
        await context.Database.EnsureCreatedAsync();

        var groupInfo = new GroupInfo("groupName", "image", "Description", "AdminHash");
        groupInfo.SetHash("AdminHash", "groupNameHash");
        
        context.GroupInfos.Add(groupInfo);
        await context.SaveChangesAsync();
        
        var member = new GroupMembers("TestUser") { GroupId = groupInfo.Id };
        member.SetHash("TestUserHash");
        
        context.GroupMembers.Add(member);
        await context.SaveChangesAsync();
        
        member.UserNickName = "UpdatedUser";

        _dbFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        // Act
        var result = await _repository.EditUserInfoAsync(member);

        // Assert
        Assert.Equal("Edit is ok", result);
    }
    
    private GroupAppDbContext GetDbContextForMy()
    {
        SQLitePCL.Batteries.Init();
        var connection = new SqliteConnection("DataSource=:memory:");
        _connections.Add(connection);
        connection.Open();
    
        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new GroupAppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
    
    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            connection.Dispose();
        }
        _connections.Clear();
    }
}