using MessagingSystem.Services.User.Persistence;
using MessagingSystem.Services.User.Persistence.User;
using Microsoft.EntityFrameworkCore;
using UserModel = MessagingSystem.Services.User.Core.User.User;

namespace MessagingSystem.Tests.User.UnitTests.Persistence.User;

public class UserRepositoryTests
{
    private async Task<AppDbContext> GetDbContextWithData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        var user = new UserModel("login", "nickname", "test");
        user.SetHashes("login_hash", "email_hash", "nick_hash");
        user.Id = "1";

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task FindUserByHashLoginAsync_ReturnsUser()
    {
        var db = await GetDbContextWithData();
        var repo = new UserRepository(db);

        var user = await repo.FindUserByHashLoginAsync("login_hash");

        Assert.NotNull(user);
        Assert.Equal("login_hash", user!.HashLogin);
    }

    [Fact]
    public async Task FindUserByHashNickNameAsync_ReturnsUser()
    {
        var db = await GetDbContextWithData();
        var repo = new UserRepository(db);

        var user = await repo.FindUserByHashNickNameAsync("nick_hash");

        Assert.NotNull(user);
        Assert.Equal("nick_hash", user!.HashNickName);
    }

    [Fact]
    public async Task FindUserByIdAsync_ReturnsUser()
    {
        var db = await GetDbContextWithData();
        var repo = new UserRepository(db);

        var user = await repo.FindUserByIdAsync("1");

        Assert.NotNull(user);
        Assert.Equal("1", user!.Id);
    }

    [Fact]
    public async Task AddUserAsync_AddsUserToDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var repo = new UserRepository(db);

        var newUser = new UserModel("login2", "nick2", "test");
        newUser.Id = "2";
        newUser.SetHashes("hash2", "email2", "nickhash2");

        await repo.AddUserAsync(newUser);

        var result = await db.Users.FindAsync("2");
        Assert.NotNull(result);
        Assert.Equal("hash2", result!.HashLogin);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUser()
    {
        var db = await GetDbContextWithData();
        var repo = new UserRepository(db);

        var user = await db.Users.FirstAsync();
        user.SetHashes("updated_login", "updated_email", "updated_nick");

        await repo.UpdateUserAsync(user);

        var updated = await db.Users.FindAsync(user.Id);
        Assert.Equal("updated_login", updated!.HashLogin);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesUser()
    {
        var db = await GetDbContextWithData();
        var repo = new UserRepository(db);

        var user = await db.Users.FirstAsync();
        await repo.DeleteUserAsync(user);

        var deleted = await db.Users.FindAsync(user.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task FindUsersByHashNickNamesAsync_ReturnsMatchingUsers()
    {
        // Arrange
        var db = await GetDbContextWithData();
        var repo = new UserRepository(db);
        var hashNickNames = new List<string> { "nick_hash" };

        // Act
        var users = await repo.FindUsersByHashNickNamesAsync(hashNickNames);

        // Assert
        Assert.NotNull(users);
        Assert.Single(users);
        Assert.Equal("nick_hash", users.First().HashNickName);
    }
}
