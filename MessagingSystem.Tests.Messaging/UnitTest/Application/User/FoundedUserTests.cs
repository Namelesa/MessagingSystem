using MessagingSystem.Services.Messaging.Application.User.Dto;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.User;

public class FoundedUserTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        const string expectedNickName = "TestUser";
        const string expectedImage = "test-image.jpg";
        
        // Act
        var user = new FoundedUser(expectedNickName, expectedImage);
        
        // Assert
        Assert.Equal(expectedNickName, user.NickName);
        Assert.Equal(expectedImage, user.Image);
    }
    
    [Fact]
    public void Constructor_ShouldHandleNullImage()
    {
        // Arrange
        const string expectedNickName = "TestUser";
        
        // Act
        var user = new FoundedUser(expectedNickName, null);
        
        // Assert
        Assert.Equal(expectedNickName, user.NickName);
        Assert.Null(user.Image);
    }
    
    [Fact]
    public void Properties_ShouldBeSettableAfterConstruction()
    {
        // Arrange
        var user = new FoundedUser("InitialName", "initial.jpg");
        const string newNickName = "UpdatedName";
        const string newImage = "updated.jpg";
        
        // Act
        user.NickName = newNickName;
        user.Image = newImage;
        
        // Assert
        Assert.Equal(newNickName, user.NickName);
        Assert.Equal(newImage, user.Image);
    }
}