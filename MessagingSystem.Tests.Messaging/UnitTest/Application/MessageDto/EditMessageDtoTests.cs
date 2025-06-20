using MessagingSystem.Services.Messaging.Application.MessageDto;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.MessageDto;

public class EditMessageDtoTests
{
    [Fact]
    public void Constructor_ShouldSetContentProperty()
    {
        // Arrange
        const string expectedContent = "Test message content";
        
        // Act
        var dto = new EditMessageDto(expectedContent);
        
        // Assert
        Assert.Equal(expectedContent, dto.Content);
    }
    
    [Fact]
    public void Content_ShouldBeSettableViaObjectInitializer()
    {
        // Arrange
        const string expectedContent = "Init content";
        
        // Act
        var dto = new EditMessageDto("constructor content") { Content = expectedContent };
        
        // Assert
        Assert.Equal(expectedContent, dto.Content);
    }
}