using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Oto.OtoMessages;

public class MessagesDtoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        const string expectedSender = "sender@test.com";
        const string expectedRecipient = "recipient@test.com";
        const string expectedContent = "Test message content";
        
        // Act
        var dto = new MessagesDto(expectedSender, expectedRecipient, expectedContent);
        
        // Assert
        Assert.Equal(expectedSender, dto.Sender);
        Assert.Equal(expectedRecipient, dto.Recipient);
        Assert.Equal(expectedContent, dto.Content);
    }
    
    [Fact]
    public void Properties_ShouldBeSettableViaObjectInitializer()
    {
        // Arrange
        const string expectedSender = "new-sender@test.com";
        const string expectedRecipient = "new-recipient@test.com";
        const string expectedContent = "New message content";
        
        // Act
        var dto = new MessagesDto("old-sender", "old-recipient", "old-content")
        {
            Sender = expectedSender,
            Recipient = expectedRecipient,
            Content = expectedContent
        };
        
        // Assert
        Assert.Equal(expectedSender, dto.Sender);
        Assert.Equal(expectedRecipient, dto.Recipient);
        Assert.Equal(expectedContent, dto.Content);
    }
}