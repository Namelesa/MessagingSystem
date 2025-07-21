using MessagingSystem.SendingModels.UserMessaging.Add;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.AddUser;

public class AddUserResponseTests
{
    [Xunit.Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_Should_Set_Success_Property_Correctly(bool expectedSuccess)
    {
        // Act
        var response = new AddUserResponse(expectedSuccess);

        // Assert
        Assert.Equal(expectedSuccess, response.Success);
    }

    [Fact]
    public void InitProperty_Should_Be_Set_Via_ObjectInitializer()
    {
        // Act
        var response = new AddUserResponse(false) 
        {
            Success = true
        };

        // Assert
        Assert.True(response.Success);
    }
}