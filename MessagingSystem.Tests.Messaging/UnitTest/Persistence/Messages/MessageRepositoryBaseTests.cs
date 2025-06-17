using FluentAssertions;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Persistence.Messages;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Persistence.Messages;

public class MessageRepositoryBaseTests
{
    [Fact]
    public async Task FindMessagesAsync_Calls_BuildQuery_And_Fetches_From_Db()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OtoAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
            .Options;

        await using var context = new OtoAppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var message1 = new Message("sender", "recipient", "foo");
        message1.SetHashes("hash1", "hash2");

        var message2 = new Message("sender", "recipient", "bar");
        message2.SetHashes("hash1", "hash2");

        context.UsersMessages.AddRange(message1, message2);
        await context.SaveChangesAsync();

        var mock = new Mock<MessageRepositoryBase<Message>>(context) { CallBase = true };
        mock.Setup(x => x.FindMessageByIdAsync(It.IsAny<Guid>())).ThrowsAsync(new NotImplementedException());

        // Act
        var messages = await mock.Object.FindMessagesAsync(new MessageFilter());

        // Assert
        messages.Should().NotBeNull();
        messages.Should().HaveCount(2);
        messages.Select(m => m.Content).Should().Contain(new[] { "foo", "bar" });

        mock.Verify(x => x.FindMessagesAsync(It.IsAny<MessageFilter>()), Times.Once);
    }
}
