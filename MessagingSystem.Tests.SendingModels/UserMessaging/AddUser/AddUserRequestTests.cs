using MessagingSystem.SendingModels.UserMessaging.Add;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.AddUser;

public class AddUserRequestTests
{
    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        // Arrange
        var expectedNick = "hashedNick123";
        var expectedImage = "base64Image";

        // Act
        var request = new AddUserRequest(expectedNick, expectedImage);

        // Assert
        Assert.Equal(expectedNick, request.NickNameHash);
        Assert.Equal(expectedImage, request.Image);
    }

    [Fact]
    public void InitProperties_Should_Be_Set_Via_ObjectInitializer()
    {
        // Act
        var request = new AddUserRequest("", "") 
        {
            NickNameHash = "initHash",
            Image = "initImage"
        };

        // Assert
        Assert.Equal("initHash", request.NickNameHash);
        Assert.Equal("initImage", request.Image);
    }
}