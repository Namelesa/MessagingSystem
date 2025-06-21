using FluentAssertions;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Persistence.Oto;

public class OtoChatRepositoryTests : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlContainer _pgContainer;
    private OtoAppDbContext _context;
    private ChatRepository _repository;

    public OtoChatRepositoryTests()
    {
        _pgContainer = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _pgContainer.StartAsync();

        var options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseNpgsql(_pgContainer.GetConnectionString())
            .Options;

        _context = new OtoAppDbContext(options);
        
        await _context.Database.MigrateAsync();

        _repository = new ChatRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _pgContainer.StopAsync();
        await _pgContainer.DisposeAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        _pgContainer.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetChatsAsync_WhenCurrentUserHasSentMessages_ReturnsChatsWithRecipients()
    {
        // Arrange
        var currentUser = "user1";
        var currentUserHash = "user1_hash";
        var recipient1 = "user2";
        var recipient1Hash = "user2_hash";
        var recipient2 = "user3";
        var recipient2Hash = "user3_hash";

        var message1 = new Message(currentUser, recipient1, "Hello User2");
        message1.SetHashes(currentUserHash, recipient1Hash);

        var message2 = new Message(currentUser, recipient2, "Hello User3");
        message2.SetHashes(currentUserHash, recipient2Hash);

        await SeedDataAsync(new[] { message1, message2 }, new[]
        {
            new UserImage(recipient1Hash, "image_user2"),
            new UserImage(recipient2Hash, "image_user3")
        });

        // Act
        var result = await _repository.GetChatsAsync(currentUserHash);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.NickName == recipient1 && c.Image == "image_user2");
        result.Should().Contain(c => c.NickName == recipient2 && c.Image == "image_user3");
    }

    [Fact]
    public async Task GetChatsAsync_WhenCurrentUserHasReceivedMessages_ReturnsChatsWithSenders()
    {
        // Arrange
        var currentUser = "user1";
        var currentUserHash = "user1_hash";
        var sender1 = "user2";
        var sender1Hash = "user2_hash";
        var sender2 = "user3";
        var sender2Hash = "user3_hash";

        var message1 = new Message(sender1, currentUser, "Hello from User2");
        message1.SetHashes(sender1Hash, currentUserHash);

        var message2 = new Message(sender2, currentUser, "Hello from User3");
        message2.SetHashes(sender2Hash, currentUserHash);

        await SeedDataAsync(new[] { message1, message2 }, new[]
        {
            new UserImage(sender1Hash, "image_user2"),
            new UserImage(sender2Hash, "image_user3")
        });

        // Act
        var result = await _repository.GetChatsAsync(currentUserHash);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.NickName == sender1 && c.Image == "image_user2");
        result.Should().Contain(c => c.NickName == sender2 && c.Image == "image_user3");
    }

    [Fact]
    public async Task GetChatsAsync_WhenCurrentUserHasBothSentAndReceivedMessages_ReturnsUniqueChats()
    {
        // Arrange
        var currentUser = "user1";
        var currentUserHash = "user1_hash";
        var otherUser = "user2";
        var otherUserHash = "user2_hash";

        var message1 = new Message(currentUser, otherUser, "Hello from User1");
        message1.SetHashes(currentUserHash, otherUserHash);

        var message2 = new Message(otherUser, currentUser, "Hello from User2");
        message2.SetHashes(otherUserHash, currentUserHash);

        await SeedDataAsync(new[] { message1, message2 }, new[]
        {
            new UserImage(otherUserHash, "image_user2")
        });

        // Act
        var result = await _repository.GetChatsAsync(currentUserHash);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].NickName.Should().Be(otherUser);
        result[0].Image.Should().Be("image_user2");
    }
    
    [Fact]
    public async Task GetChatsAsync_WhenDeletedMessagesExist_ShouldIgnoreDeletedMessages()
    {
        // Arrange
        var currentUserHash = "user1_hash";
        var otherUserHash = "user2_hash";

        var activeMessage = new Message("user1", "user2", "Active message");
        activeMessage.SetHashes(currentUserHash, otherUserHash);

        var deletedMessage = new Message("user1", "user3", "Deleted message");
        deletedMessage.SetHashes(currentUserHash, "user3_hash");
        deletedMessage.SoftDeleteInfo();

        await SeedDataAsync(new[] { activeMessage, deletedMessage }, new[]
        {
            new UserImage(otherUserHash, "image_user2")
        });

        // Act
        var result = await _repository.GetChatsAsync(currentUserHash);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].NickName.Should().Be("user2");
    }

    [Fact]
    public async Task GetChatsAsync_WhenNoMessages_ReturnsEmptyList()
    {
        // Arrange
        var currentUserHash = "nonexistent_hash";

        // Act
        var result = await _repository.GetChatsAsync(currentUserHash);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    private async Task SeedDataAsync(Message[] messages, UserImage[] images)
    {
        await _context.UsersMessages.AddRangeAsync(messages);
        await _context.Images.AddRangeAsync(images);
        await _context.SaveChangesAsync();
    }
}