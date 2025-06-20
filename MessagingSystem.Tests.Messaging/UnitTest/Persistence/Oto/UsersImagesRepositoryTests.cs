using FluentAssertions;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.UsersImages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Oto;

public class UserImageRepositoryTests : IDisposable
{
    private readonly DbContextOptions<OtoAppDbContext> _options;
    private readonly UserImageRepository _repository;

    public UserImageRepositoryTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var contextFactory = new TestDbContextFactory(_options);
        _repository = new UserImageRepository(contextFactory);
    }

    #region FindUserImageByHashAsync Tests

    [Fact]
    public async Task FindUserImageByHashAsync_WhenUserImageExists_ReturnsUserImage()
    {
        // Arrange
        var nicknameHash = "test_hash";
        var userImage = new UserImage(nicknameHash, "test_image_data");
        
        await SeedDataAsync(userImage);

        // Act
        var result = await _repository.FindUserImageByHashAsync(nicknameHash);

        // Assert
        result.Should().NotBeNull();
        result.NickNameHash.Should().Be(nicknameHash);
        result.Image.Should().Be("test_image_data");
    }

    [Fact]
    public async Task FindUserImageByHashAsync_WhenUserImageDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nicknameHash = "non_existent_hash";

        // Act
        var result = await _repository.FindUserImageByHashAsync(nicknameHash);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindUserImageByHashAsync_WithNullHash_ReturnsNull()
    {
        // Act
        var result = await _repository.FindUserImageByHashAsync(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindUserImageByHashAsync_WithEmptyHash_ReturnsNull()
    {
        // Act
        var result = await _repository.FindUserImageByHashAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindUserImageByHashAsync_WithMultipleImages_ReturnsCorrectOne()
    {
        // Arrange
        var targetHash = "target_hash";
        var userImage1 = new UserImage("hash1", "image1");
        var userImage2 = new UserImage(targetHash, "target_image");
        var userImage3 = new UserImage("hash3", "image3");
        
        await SeedDataAsync(userImage1, userImage2, userImage3);

        // Act
        var result = await _repository.FindUserImageByHashAsync(targetHash);

        // Assert
        result.Should().NotBeNull();
        result.NickNameHash.Should().Be(targetHash);
        result.Image.Should().Be("target_image");
    }

    #endregion

    #region AddUserImageAsync Tests

    [Fact]
    public async Task AddUserImageAsync_WithValidUserImage_AddsAndReturnsUserImage()
    {
        // Arrange
        var userImage = new UserImage("test_hash", "test_image");

        // Act
        var result = await _repository.AddUserImageAsync(userImage);

        // Assert
        result.Should().NotBeNull();
        result.NickNameHash.Should().Be("test_hash");
        result.Image.Should().Be("test_image");
        
        var savedImage = await FindImageInDatabaseAsync("test_hash");
        savedImage.Should().NotBeNull();
        savedImage.Image.Should().Be("test_image");
    }

    [Fact]
    public async Task AddUserImageAsync_WithNullUserImage_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.AddUserImageAsync(null));
    }
    

    [Fact]
    public async Task AddUserImageAsync_GeneratesUniqueId()
    {
        // Arrange
        var userImage1 = new UserImage("hash1", "image1");
        var userImage2 = new UserImage("hash2", "image2");

        // Act
        var result1 = await _repository.AddUserImageAsync(userImage1);
        var result2 = await _repository.AddUserImageAsync(userImage2);

        // Assert
        result1.Id.Should().NotBe(Guid.Empty);
        result2.Id.Should().NotBe(Guid.Empty);
        result1.Id.Should().NotBe(result2.Id);
    }

    #endregion

    #region EditUserImageAsync Tests

    [Fact]
    public async Task EditUserImageAsync_WithExistingUserImage_UpdatesAndReturnsUserImage()
    {
        // Arrange
        var originalImage = new UserImage("test_hash", "original_image");
        await SeedDataAsync(originalImage);
        
        var savedImage = await FindImageInDatabaseAsync("test_hash");
        savedImage.Should().NotBeNull();
        
        savedImage.EditInfo("updated_hash", "updated_image");

        // Act
        var result = await _repository.EditUserImageAsync(savedImage);

        // Assert
        result.Should().NotBeNull();
        result.NickNameHash.Should().Be("updated_hash");
        result.Image.Should().Be("updated_image");
        
        // Verify it was updated in database
        var updatedImage = await FindImageByIdInDatabaseAsync(savedImage.Id);
        updatedImage.Should().NotBeNull();
        updatedImage.NickNameHash.Should().Be("updated_hash");
        updatedImage.Image.Should().Be("updated_image");
    }

    [Fact]
    public async Task EditUserImageAsync_WithNullUserImage_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _repository.EditUserImageAsync(null));
    }

    [Fact]
    public async Task EditUserImageAsync_UpdatesOnlySpecifiedFields()
    {
        // Arrange
        var originalImage = new UserImage("original_hash", "original_image");
        await SeedDataAsync(originalImage);
        
        var savedImage = await FindImageInDatabaseAsync("original_hash");
        var originalId = savedImage.Id;
        
        savedImage.EditInfo("updated_hash", "updated_image");

        // Act
        var result = await _repository.EditUserImageAsync(savedImage);

        // Assert
        result.Id.Should().Be(originalId); 
        result.NickNameHash.Should().Be("updated_hash");
        result.Image.Should().Be("updated_image");
    }

    #endregion

    #region DeleteUserImageAsync Tests
    
    [Fact]
    public async Task DeleteUserImageAsync_WithNullUserImage_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _repository.DeleteUserImageAsync(null));
    }

    [Fact]
    public async Task DeleteUserImageAsync_WithNonExistentUserImage_ThrowsException()
    {
        // Arrange
        var nonExistentImage = new UserImage("non_existent_hash", "image");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.DeleteUserImageAsync(nonExistentImage));
    }
    
    [Fact]
    public async Task DeleteUserImageAsync_ValidUserImage_RemovesAndReturnsOne()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseSqlite(connection)
            .Options;
        
        await using (var setupContext = new OtoAppDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            var user = new UserImage("nick", "img");
            user.EditInfo("hash123", "img");
            await setupContext.Images.AddAsync(user);
            await setupContext.SaveChangesAsync();
        }
        
        var dbFactoryMock = new Mock<IDbContextFactory<OtoAppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new OtoAppDbContext(options)); 

        var repo = new UserImageRepository(dbFactoryMock.Object);

        var userImage = new UserImage("nick", "img");
        userImage.EditInfo("hash123", "img");

        // Act
        var result = await repo.DeleteUserImageAsync(userImage);

        // Assert
        Assert.Equal(1, result); 
        
        await using var verifyContext = new OtoAppDbContext(options);
        var remaining = await verifyContext.Images.ToListAsync();
        Assert.Empty(remaining);

        await connection.CloseAsync();
    }
    
    [Fact]
    public async Task DeleteUserImageAsync_NoMatchingUserImage_ReturnsZero()
    {
        // Arrange
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new OtoAppDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        var dbFactoryMock = new Mock<IDbContextFactory<OtoAppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new OtoAppDbContext(options));

        var repo = new UserImageRepository(dbFactoryMock.Object);

        var userImage = new UserImage("ghost", "img");
        userImage.EditInfo("nonexistent-hash", "img");

        // Act
        var result = await repo.DeleteUserImageAsync(userImage);

        // Assert
        Assert.Equal(0, result);

        await connection.CloseAsync();
    }
    
    [Fact]
    public async Task DeleteUserImageAsync_WhenTableDoesNotExist_ThrowsSqliteException()
    {
        // Arrange
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var dbFactoryMock = new Mock<IDbContextFactory<OtoAppDbContext>>();
        dbFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new OtoAppDbContext(options));

        var repo = new UserImageRepository(dbFactoryMock.Object);

        var userImage = new UserImage("nick", "img");
        userImage.EditInfo("hash123", "img");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SqliteException>(() => repo.DeleteUserImageAsync(userImage));
        Assert.Contains("no such table", ex.Message, StringComparison.OrdinalIgnoreCase);

        await connection.CloseAsync();
    }

    #endregion
    
    #region Helper Methods

    private async Task SeedDataAsync(params UserImage[] images)
    {
        await using var context = new OtoAppDbContext(_options);
        context.Images.AddRange(images);
        await context.SaveChangesAsync();
    }

    private async Task<UserImage?> FindImageInDatabaseAsync(string nicknameHash)
    {
        await using var context = new OtoAppDbContext(_options);
        return await context.Images.FirstOrDefaultAsync(i => i.NickNameHash == nicknameHash);
    }

    private async Task<UserImage?> FindImageByIdInDatabaseAsync(Guid id)
    {
        await using var context = new OtoAppDbContext(_options);
        return await context.Images.FirstOrDefaultAsync(i => i.Id == id);
    }
    
    #endregion

    public void Dispose()
    {
        // Cleanup is handled by InMemory database disposal
    }
}

public class TestDbContextFactory(DbContextOptions<OtoAppDbContext> options) : IDbContextFactory<OtoAppDbContext>
{
    public OtoAppDbContext CreateDbContext()
    {
        return new OtoAppDbContext(options);
    }

    public Task<OtoAppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}