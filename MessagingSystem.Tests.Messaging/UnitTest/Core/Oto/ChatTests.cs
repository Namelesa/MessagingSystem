using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Core.Oto;

public class ChatTests
{
    [Fact]
    public void Chat_Properties_Can_Be_Set_And_Get()
    {
        // Arrange
        var chat = new Chat
        {
            // Act
            NickName = "Username",
            Image = "http://example.com/image.jpg"
        };

        // Assert
        Assert.Equal("Username", chat.NickName);
        Assert.Equal("http://example.com/image.jpg", chat.Image);
    }
}