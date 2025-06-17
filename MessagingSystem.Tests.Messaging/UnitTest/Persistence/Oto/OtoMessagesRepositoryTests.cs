using FluentAssertions;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoMessages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Oto;

public class OtoMessageRepositoryTests : IDisposable
{
    private readonly OtoAppDbContext _context;
    private readonly OtoMessageRepository _repository;

    public OtoMessageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OtoAppDbContext(options);
        _repository = new OtoMessageRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region FindMessageByIdAsync Tests

    [Fact]
    public async Task FindMessageByIdAsync_WhenMessageExists_ShouldReturnMessage()
    {
        // Arrange
        var message = new Message("sender", "recipient", "test content");
        message.SetHashes("senderHash", "recipientHash");
        
        _context.UsersMessages.Add(message);
        await _context.SaveChangesAsync();

        var messageId = message.Id;
        
        // Act
        var result = await _repository.FindMessageByIdAsync(messageId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(messageId);
        result.Content.Should().Be("test content");
    }

    [Fact]
    public async Task FindMessageByIdAsync_WhenMessageDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.FindMessageByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateMessageAsync Tests

    [Fact]
    public async Task CreateMessageAsync_WhenValidMessage_ShouldCreateAndReturnMessage()
    {
        // Arrange
        var message = new Message("sender", "recipient", "test content");
        message.SetHashes("senderHash", "recipientHash");
        
        // Act
        var result = await _repository.CreateMessageAsync(message);

        var originalId = result.Id;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(originalId);
        result.Content.Should().Be("test content");
        result.Sender.Should().Be("sender");
        result.Recipient.Should().Be("recipient");
    }

    [Fact]
    public async Task CreateMessageAsync_WhenNullMessage_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _repository.CreateMessageAsync(null));
    }

    #endregion

    #region EditMessageAsync Tests

    [Fact]
    public async Task EditMessageAsync_WhenValidMessage_ShouldUpdateAndReturnMessage()
    {
        // Arrange
        var message = new Message("sender", "recipient", "original content");
        message.SetHashes("senderHash", "recipientHash");
        _context.UsersMessages.Add(message);
        await _context.SaveChangesAsync();

        message.EditInfo("edited content");

        // Act
        var result = await _repository.EditMessageAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("edited content");
        result.IsEdited.Should().BeTrue();
        result.EditDate.Should().NotBeNull();

        // Verify it was updated in database
        var updatedMessage = await _context.UsersMessages.FindAsync(message.Id);
        updatedMessage?.Content.Should().Be("edited content");
        updatedMessage?.IsEdited.Should().BeTrue();
    }
    
    #endregion

    #region DeleteMessageAsync Tests

    [Fact]
    public async Task DeleteMessageAsync_WhenValidMessage_ShouldDeleteAndReturnMessage()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");
        message.SetHashes("senderHash", "recipientHash");
        _context.UsersMessages.Add(message);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteMessageAsync(message);

        // Assert
        result.Should().NotBeNull();

        // Verify it was deleted from database
        var deletedMessage = await _context.UsersMessages.FindAsync(message.Id);
        deletedMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenMessageDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => 
            _repository.DeleteMessageAsync(message));
    }

    #endregion

    #region SoftDeleteMessageAsync Tests

    [Fact]
    public async Task SoftDeleteMessageAsync_WhenValidMessage_ShouldMarkAsDeletedAndReturnMessage()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");
        message.SetHashes("senderHash", "recipientHash");
        _context.UsersMessages.Add(message);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SoftDeleteMessageAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.IsDeleted.Should().BeTrue();

        // Verify it was soft deleted in database
        var softDeletedMessage = await _context.UsersMessages.FindAsync(message.Id);
        softDeletedMessage.Should().NotBeNull();
        softDeletedMessage.IsDeleted.Should().BeTrue();
    }
    
    #endregion

    #region UpdateUserHashesAsync Tests

    [Fact]
    public async Task UpdateUserHashesAsync_WhenMessagesExist_ShouldUpdateHashesAndReturnCount()
    {
        // Arrange
        var options = CreateRelationalOptions();
        await using var context = new OtoAppDbContext(options);
        
        var message1 = new Message("oldUser", "recipient1", "content1");
        var message2 = new Message("sender2", "oldUser", "content2");
        var message3 = new Message("otherUser", "recipient3", "content3");
        
        message1.SetHashes("oldHash", "recipientHash1");
        message2.SetHashes("senderHash2", "oldHash");
        message3.SetHashes("otherHash", "recipientHash3");

        await context.Database.EnsureCreatedAsync();
        context.UsersMessages.AddRange(message1, message2, message3);
        await context.SaveChangesAsync();
        
        var repository = new OtoMessageRepository(context);
        
        // Act
        var result = await repository.UpdateUserHashesAsync("oldHash", "newUser", "newHash");

        // Assert
        result.Should().Be(2); 
    }

    [Fact]
    public async Task UpdateUserHashesAsync_WhenNoMessagesMatch_ShouldReturnZero()
    {
        // Arrange
        var options = CreateRelationalOptions();
        await using var context = new OtoAppDbContext(options);
        
        var message = new Message("user", "recipient", "content");
        message.SetHashes("hash", "recipientHash");
        
        await context.Database.EnsureCreatedAsync();
        await context.UsersMessages.AddAsync(message);
        await context.SaveChangesAsync();
        
        var repository = new OtoMessageRepository(context);

        // Act
        var result = await repository.UpdateUserHashesAsync("nonExistentHash", "newUser", "newHash");

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region DeleteUserHashesAsync Tests
    
    [Fact]
    public async Task DeleteUserHashesAsync_WhenMessagesExist_ShouldDeleteMessagesAndReturnCount()
    {
        // Arrange
        var options = CreateRelationalOptions();
        await using var context = new OtoAppDbContext(options);
        var message1 = new Message("targetUser", "recipient1", "content1");
        var message2 = new Message("sender2", "targetUser", "content2");
        var message3 = new Message("otherUser", "recipient3", "content3");

        message1.SetHashes("targetHash", "recipientHash1");
        message2.SetHashes("senderHash2", "targetHash");
        message3.SetHashes("otherHash", "recipientHash3");
        
        await context.Database.EnsureCreatedAsync();
        context.UsersMessages.AddRange(message1, message2, message3);
        await context.SaveChangesAsync();
        
        var repository = new OtoMessageRepository(context);
        
        // Act
        var result = await repository.DeleteUserHashesAsync("targetHash");

        // Assert
        result.Should().Be(2);
        
        var remainingMessages = await context.UsersMessages.ToListAsync();
        remainingMessages.Should().HaveCount(1);
        remainingMessages.First().Id.Should().Be(message3.Id);
    }

    [Fact]
    public async Task DeleteUserHashesAsync_WhenNoMessagesMatch_ShouldReturnZero()
    {
        // Arrange
        var options = CreateRelationalOptions();
        await using var context = new OtoAppDbContext(options);
        
        var message = new Message("user", "recipient", "content");
        message.SetHashes("hash", "recipientHash");
        
        await context.Database.EnsureCreatedAsync();
        await context.UsersMessages.AddAsync(message);
        await context.SaveChangesAsync();
        
        var repository = new OtoMessageRepository(context);

        // Act
        var result = await repository.DeleteUserHashesAsync("nonExistentHash");

        // Assert
        result.Should().Be(0);

        // Verify no messages were deleted
        var remainingMessages = await context.UsersMessages.ToListAsync();
        remainingMessages.Should().HaveCount(1);
    }

    #endregion

    #region GetMessageStoryAsync Tests

    [Fact]
    public async Task GetMessageStoryAsync_WhenConversationExists_ShouldReturnMessagesInDescendingOrder()
    {
        // Arrange
        var user1Hash = "user1Hash";
        var user2Hash = "user2Hash";
        var otherUserHash = "otherUserHash";

        var message1 = new Message("user1", "user2", "First message");
        var message2 = new Message("user2", "user1", "Second message");
        var message3 = new Message("user1", "user2", "Third message");
        var otherMessage = new Message("otherUser", "user2", "Other conversation");

        message1.SetHashes(user1Hash, user2Hash);
        message2.SetHashes(user2Hash, user1Hash);
        message3.SetHashes(user1Hash, user2Hash);
        otherMessage.SetHashes(otherUserHash, user2Hash);

        _context.UsersMessages.AddRange(message1, message2, message3, otherMessage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMessageStoryAsync(user1Hash, user2Hash, 10);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(m => 
            (m.SenderHash == user1Hash && m.RecipientHash == user2Hash) ||
            (m.SenderHash == user2Hash && m.RecipientHash == user1Hash));
        
        // Should be ordered by SendTime descending (newest first)
        result.Should().BeInDescendingOrder(m => m.SendTime);
    }

    [Fact]
    public async Task GetMessageStoryAsync_WhenTakeLimitIsSmaller_ShouldReturnLimitedResults()
    {
        // Arrange
        var user1Hash = "user1Hash";
        var user2Hash = "user2Hash";

        var messages = new List<Message>();
        for (int i = 0; i < 5; i++)
        {
            var message = new Message($"user{i % 2 + 1}", $"user{(i + 1) % 2 + 1}", $"Message {i}");
            message.SetHashes(i % 2 == 0 ? user1Hash : user2Hash, i % 2 == 0 ? user2Hash : user1Hash);
            messages.Add(message);
        }

        _context.UsersMessages.AddRange(messages);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMessageStoryAsync(user1Hash, user2Hash, 3);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(m => m.SendTime);
    }

    [Fact]
    public async Task GetMessageStoryAsync_WhenNoConversationExists_ShouldReturnEmptyList()
    {
        // Arrange
        var user1Hash = "user1Hash";
        var user2Hash = "user2Hash";

        var otherMessage = new Message("otherUser1", "otherUser2", "Other conversation");
        otherMessage.SetHashes("otherHash1", "otherHash2");
        _context.UsersMessages.Add(otherMessage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMessageStoryAsync(user1Hash, user2Hash, 10);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ReplyMessageAsync Tests

    [Fact]
    public async Task ReplyMessageAsync_WhenValidMessage_ShouldSetReplyAndReturnMessage()
    {
        // Arrange
        var originalMessage = new Message("sender1", "recipient1", "Original message");
        originalMessage.SetHashes("senderHash", "recipientHash");
        var replyMessage = new Message("recipient1", "sender1", "Reply message");
        replyMessage.SetHashes("recipientHash", "senderHash");
        
        _context.UsersMessages.AddRange(originalMessage, replyMessage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ReplyMessageAsync(originalMessage.Id, replyMessage);

        // Assert
        result.Should().NotBeNull();
        result.ReplyFor.Should().Be(originalMessage.Id);

        // Verify it was updated in database
        var updatedMessage = await _context.UsersMessages.FindAsync(replyMessage.Id);
        updatedMessage?.ReplyFor.Should().Be(originalMessage.Id);
    }
    
    #endregion
    
    private DbContextOptions<OtoAppDbContext> CreateRelationalOptions()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        return new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseSqlite(connection)
            .Options;
    }
}