using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Group;

public class GroupMessagesRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GroupAppDbContext _context;
    private readonly GroupMessagesRepository _repository;

    public GroupMessagesRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GroupAppDbContext(options);
        
        _context.Database.EnsureCreated();
        
        _repository = new GroupMessagesRepository(_context);
    }

    [Fact]
    public async Task FindMessageByIdAsync_WhenMessageExists_ReturnsMessage()
    {
        // Arrange
        await _context.Database.ExecuteSqlRawAsync(@"
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
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var message = new GroupMessage("TestSender", "Test content")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        
        message.SetHashes("testHash123");
        
        await _context.GroupMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindMessageByIdAsync(message.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(message.Id, result.Id);
        Assert.Equal(message.Sender, result.Sender);
        Assert.Equal(message.Content, result.Content);
    }

    [Fact]
    public async Task FindMessageByIdAsync_WhenMessageDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.FindMessageByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateMessageAsync_WhenValidMessage_CreatesAndReturnsMessage()
    {
        // Arrange
        await _context.Database.ExecuteSqlRawAsync(@"
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
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var message = new GroupMessage("TestSender", "Test content")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        
        message.SetHashes("testHash123");

        // Act
        var result = await _repository.CreateMessageAsync(message);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(message.Id, result.Id);
        
        // Verify message was saved to database
        var savedMessage = await _context.GroupMessages.FindAsync(message.Id);
        Assert.NotNull(savedMessage);
        Assert.Equal(message.Sender, savedMessage.Sender);
        Assert.Equal(message.Content, savedMessage.Content);
    }

    [Fact]
    public async Task EditMessageAsync_WhenMessageExists_UpdatesAndReturnsMessage()
    {
        // Arrange
        await _context.Database.ExecuteSqlRawAsync(@"
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
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var message = new GroupMessage("TestSender", "Test content")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        
        message.SetHashes("testHash123");
        
        await _context.GroupMessages.AddAsync(message);
        await _context.SaveChangesAsync();
        
        _context.Entry(message).State = EntityState.Detached;
        
        message.EditInfo("Updated content");

        // Act
        var result = await _repository.EditMessageAsync(message);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated content", result.Content);
        Assert.True(result.IsEdited);
        Assert.NotNull(result.EditTime);
        
        var updatedMessage = await _context.GroupMessages.FindAsync(message.Id);
        Assert.Equal("Updated content", updatedMessage?.Content);
        Assert.True(updatedMessage?.IsEdited);
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenMessageExists_DeletesAndReturnsMessage()
    {
        // Arrange
        await _context.Database.ExecuteSqlRawAsync(@"
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
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var message = new GroupMessage("TestSender", "Test content")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        
        message.SetHashes("testHash123");
        
        await _context.GroupMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        // Detach the entity to avoid tracking conflicts
        _context.Entry(message).State = EntityState.Detached;

        // Act
        var result = await _repository.DeleteMessageAsync(message);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(message.Id, result.Id);

        // Verify message was deleted from database
        var deletedMessage = await _context.GroupMessages.FindAsync(message.Id);
        Assert.Null(deletedMessage);
    }

    [Fact]
    public async Task SoftDeleteMessageAsync_WhenMessageExists_SoftDeletesAndReturnsMessage()
    {
        // Arrange
        await _context.Database.ExecuteSqlRawAsync(@"
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
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var message = new GroupMessage("TestSender", "Test content")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        
        message.SetHashes("testHash123");
        
        await _context.GroupMessages.AddAsync(message);
        await _context.SaveChangesAsync();
        
        _context.Entry(message).State = EntityState.Detached;

        // Act
        var result = await _repository.SoftDeleteMessageAsync(message);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task UpdateUserHashesAsync_WhenMessagesExist_UpdatesMatchingMessages()
    {
        // Arrange
        const string oldHash = "oldHash123";
        const string newNick = "NewNickname";
        const string newHash = "newHash456";

        await _context.Database.ExecuteSqlRawAsync(@"
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
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var message1 = new GroupMessage("OldSender", "Content 1")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        message1.SetHashes(oldHash);

        var message2 = new GroupMessage("OldSender", "Content 2")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        message2.SetHashes(oldHash);

        var message3 = new GroupMessage("DifferentSender", "Content 3")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        message3.SetHashes("differentHash");

        await _context.GroupMessages.AddRangeAsync(message1, message2, message3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UpdateUserHashesAsync(oldHash, newNick, newHash);

        // Assert
        Assert.Equal(2, result);

        // Verify updates
        _context.ChangeTracker.Clear();
        var updatedMessages = await _context.GroupMessages
            .Where(m => m.SenderHash == newHash)
            .ToListAsync();
        
        Assert.Equal(2, updatedMessages.Count);
        Assert.All(updatedMessages, m => 
        {
            Assert.Equal(newNick, m.Sender);
            Assert.Equal(newHash, m.SenderHash);
        });
    }

    [Fact]
    public async Task UpdateUserHashesAsync_WhenNoMatchingMessages_ReturnsZero()
    {
        // Arrange
        const string nonExistentHash = "nonExistentHash";
        const string newNick = "NewNickname";
        const string newHash = "newHash456";

        // Act
        var result = await _repository.UpdateUserHashesAsync(nonExistentHash, newNick, newHash);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task DeleteUserHashesAsync_WhenMessagesExist_DeletesMatchingMessages()
    {
        // Arrange
        const string targetHash = "targetHash123";
        const string keepHash = "keepHash456";

        await _context.Database.ExecuteSqlRawAsync(@"
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
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var message1 = new GroupMessage("Sender1", "Content 1")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        message1.SetHashes(targetHash);

        var message2 = new GroupMessage("Sender2", "Content 2")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        message2.SetHashes(targetHash);

        var message3 = new GroupMessage("Sender3", "Content 3")
        {
            Id = Guid.NewGuid(),
            GroupId = groupGuid
        };
        message3.SetHashes(keepHash);

        await _context.GroupMessages.AddRangeAsync(message1, message2, message3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteUserHashesAsync(targetHash);

        // Assert
        Assert.Equal(2, result); 

        // Verify deletions
        var remainingMessages = await _context.GroupMessages.ToListAsync();
        Assert.Single(remainingMessages);
        Assert.Equal(keepHash, remainingMessages[0].SenderHash);
    }

    [Fact]
    public async Task DeleteUserHashesAsync_WhenNoMatchingMessages_ReturnsZero()
    {
        // Arrange
        var nonExistentHash = "nonExistentHash";

        // Act
        var result = await _repository.DeleteUserHashesAsync(nonExistentHash);

        // Assert
        Assert.Equal(0, result);
    }
    
    [Fact]
    public async Task GetMessageStoryAsync_WhenTakeParameterLimits_ReturnsLimitedMessages()
    {
        // Arrange
        var messages = new List<GroupMessage>();
        var baseTime = DateTime.UtcNow;
        
        await _context.Database.ExecuteSqlRawAsync(@"
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

        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        for (var i = 0; i < 5; i++)
        {
            var message = new GroupMessage($"Sender{i}", $"Content {i}")
            {
                GroupId = groupGuid,
                SendTime = baseTime.AddMinutes(-i)
            };
            message.SetHashes($"testHash{i}");
            messages.Add(message);
        }

        await _context.GroupMessages.AddRangeAsync(messages);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMessageStoryAsync(groupGuid, 2, 20);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, m => Assert.Equal(groupGuid, m.GroupId));
    }

    [Fact]
    public async Task GetMessageStoryAsync_WhenNoMessages_ReturnsEmptyList()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        var result = await _repository.GetMessageStoryAsync(groupId, 1, 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReplyMessageAsync_WhenValidReplyId_SetsReplyAndReturnsMessage()
    {
        // Arrange
        await _context.Database.ExecuteSqlRawAsync(@"
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
        
        await _context.SaveChangesAsync();
        
        var groupGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var originalMessage = new GroupMessage("OriginalSender", "Original content")
        {
            GroupId = groupGuid
        };
        originalMessage.SetHashes("testHash123");
        
        await _context.GroupMessages.AddAsync(originalMessage);
        await _context.SaveChangesAsync();
        
        _context.Entry(originalMessage).State = EntityState.Detached;

        var replyMessage = new GroupMessage("ReplySender", "Reply content")
        {
            GroupId = originalMessage.GroupId
        };
        replyMessage.SetHashes("testHash456");

        await _context.GroupMessages.AddAsync(replyMessage);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.ReplyMessageAsync(originalMessage.Id, replyMessage);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(originalMessage.Id, result.ReplyFor);
        var savedReplyMessage = await _context.GroupMessages.FindAsync(replyMessage.Id);
        Assert.Equal(originalMessage.Id, savedReplyMessage?.ReplyFor);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}