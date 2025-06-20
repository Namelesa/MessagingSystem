using MessagingSystem.Services.Messaging.Application.MessageDto;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.MessageDto;

public class CreatedMessageResultTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid();
        var expectedSentTime = DateTime.UtcNow;
        
        // Act
        var result = new CreatedMessageResult(expectedMessageId, expectedSentTime);
        
        // Assert
        Assert.Equal(expectedMessageId, result.MessageId);
        Assert.Equal(expectedSentTime, result.SentTime);
    }
    
    [Fact]
    public void Properties_ShouldBeSettableViaObjectInitializer()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid();
        var expectedSentTime = DateTime.UtcNow;
        
        // Act
        var result = new CreatedMessageResult(Guid.Empty, DateTime.MinValue) 
        { 
            MessageId = expectedMessageId,
            SentTime = expectedSentTime
        };
        
        // Assert
        Assert.Equal(expectedMessageId, result.MessageId);
        Assert.Equal(expectedSentTime, result.SentTime);
    }
}