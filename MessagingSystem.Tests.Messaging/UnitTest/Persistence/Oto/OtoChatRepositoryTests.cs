using FluentAssertions;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Oto;

public class OtoChatRepositoryTests : IDisposable
{
    private readonly OtoAppDbContext _context;
    private readonly ChatRepository _repository;

    public OtoChatRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OtoAppDbContext(options);
        _repository = new ChatRepository(_context);
    }

    [Fact]
    public async Task GetChatsAsync_WhenCurrentUserHasSentMessages_ReturnsChatsWithRecipients()
    {
        // Arrange
        var currentUser = "user1";
        var recipient1 = "user2";
        var recipient2 = "user3";

        var message1 = new Message(currentUser, recipient1, "Hello User2");
        message1.SetHashes("user1", "user2");
        
        var message2 = new Message(currentUser, recipient2, "Hello User3");
        message2.SetHashes("user1", "user3");
        _context.UsersMessages.AddRange(message1, message2);

        _context.Images.AddRange(
            new UserImage(recipient1, "image_user2"),
            new UserImage(recipient2, "image_user3")
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetChatsAsync(currentUser);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.NickName == recipient1);
        result.Should().Contain(c => c.NickName == recipient2);
        result.All(c => !string.IsNullOrEmpty(c.Image)).Should().BeTrue();
    }

    [Fact]
    public async Task GetChatsAsync_WhenCurrentUserHasReceivedMessages_ReturnsChatsWithSenders()
    {
        // Arrange
        var currentUser = "user1";
        var sender1 = "user2";
        var sender2 = "user3";

        var message1 = new Message(sender1, currentUser, "Hello from User2");
        message1.SetHashes("user2", "user1");
        
        var message2 = new Message(sender2, currentUser, "Hello from User3");
        message2.SetHashes("user2", "user1");
        
        _context.UsersMessages.AddRange(message1, message2);

        _context.Images.AddRange(
            new UserImage(sender1, "image_user2"),
            new UserImage(sender2, "image_user3")
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetChatsAsync(currentUser);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.NickName == sender1);
        result.Should().Contain(c => c.NickName == sender2);
        result.All(c => !string.IsNullOrEmpty(c.Image)).Should().BeTrue();
    }

    [Fact]
    public async Task GetChatsAsync_WhenCurrentUserHasBothSentAndReceivedMessages_ReturnsUniqueChats()
    {
        // Arrange
        var currentUser = "user1";
        var otherUser = "user2";

        // User1 sends to User2
        var message1 = new Message(currentUser, otherUser, "test");
        message1.SetHashes("user1", "user2");
        _context.UsersMessages.Add(message1);

        // User2 sends to User1
        var message2 = new Message(currentUser, otherUser, "test");
        message2.SetHashes("user2", "user1");
        _context.UsersMessages.Add(message2);

        // Add image for otherUser
        _context.Images.Add(new UserImage(otherUser, "image_user2"));

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetChatsAsync(currentUser);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().NickName.Should().Be(otherUser);
        result.First().Image.Should().Be("image_user2");
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}