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

        _connection = new SqliteConnection("DataSource=file:memdb1?mode=memory&cache=shared");
        _connection.Open();

        _dbFactoryMock = new Mock<IDbContextFactory<GroupAppDbContext>>();
        _repository = new GroupMemberRepository(_dbFactoryMock.Object);
    }

    private GroupAppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        return new GroupAppDbContext(options);
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
    public async Task EditUserInfoAsync_WithValidGroupMember_ReturnsSuccessMessage()
    {
        // Arrange
        var context = GetDbContext();
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
        
        _dbFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GetDbContext());
        
        var existingMember = await context.GroupMembers.FirstAsync();
        var memberId = existingMember.Id;
        existingMember.UserNickName = "UpdatedNickName";

        // Act
        var result = await _repository.EditUserInfoAsync(existingMember);

        // Assert
        Assert.Equal("Edit is ok", result);

        await using var verificationContext = GetDbContext();
        var updatedMember = await verificationContext.GroupMembers.FindAsync(memberId);
        Assert.Equal("UpdatedNickName", updatedMember?.UserNickName);
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
        await using (var context = GetDbContext())
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
        Assert.True(result > 0);

        await using (var context = GetDbContext())
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

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            connection.Dispose();
        }
        _connections.Clear();
    }
}