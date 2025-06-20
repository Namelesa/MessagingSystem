using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Oto.OtoChats;

public class ChatDtoTests
{
    [Fact]
    public void Properties_ShouldBeSettableViaObjectInitializer()
    {
        // Arrange
        const string expectedNickName = "TestUser";
        const string expectedImage = "test-image.jpg";
        
        // Act
        var dto = new ChatDto
        {
            NickName = expectedNickName,
            Image = expectedImage
        };
        
        // Assert
        Assert.Equal(expectedNickName, dto.NickName);
        Assert.Equal(expectedImage, dto.Image);
    }
}