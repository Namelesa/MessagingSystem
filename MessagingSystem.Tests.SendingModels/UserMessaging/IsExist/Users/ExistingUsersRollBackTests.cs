using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.IsExist.Users;

public class ExistingUsersRollBackTests
{
    [Fact]
    public void Constructor_WithValidUsersList_SetsPropertyCorrectly()
    {
        // Arrange
        var expectedUsers = new List<ExistingUserDto>
        {
            new("user1", true, "image1.jpg"),
            new("user2", false, "image2.jpg")
        };

        // Act
        var response = new ExistingUsersResponse(expectedUsers);

        // Assert
        Assert.Equal(expectedUsers, response.Users);
        Assert.Equal(2, response.Users.Count);
    }

    [Fact]
    public void Constructor_WithEmptyList_SetsPropertyToEmptyList()
    {
        // Arrange
        var emptyList = new List<ExistingUserDto>();

        // Act
        var response = new ExistingUsersResponse(emptyList);

        // Assert
        Assert.Equal(emptyList, response.Users);
        Assert.Empty(response.Users);
    }

    [Fact]
    public void Constructor_WithNullValue_SetsPropertyToNull()
    {
        // Arrange
        List<ExistingUserDto>? nullList = null;

        // Act
        var response = new ExistingUsersResponse(nullList);

        // Assert
        Assert.Null(response.Users);
    }

    [Fact]
    public void Users_Property_CanBeModified()
    {
        // Arrange
        var initialUsers = new List<ExistingUserDto>
        {
            new ExistingUserDto("user1", true, "image1.jpg")
        };
        var newUsers = new List<ExistingUserDto>
        {
            new("user2", false, "image2.jpg"),
            new("user3", true, "image3.jpg")
        };
        var response = new ExistingUsersResponse(initialUsers)
        {
            // Act
            Users = newUsers
        };

        // Assert
        Assert.Equal(newUsers, response.Users);
        Assert.Equal(2, response.Users.Count);
    }

    [Fact]
    public void Users_Property_CanBeSetToNull()
    {
        // Arrange
        var initialUsers = new List<ExistingUserDto>
        {
            new("user1", true, "image1.jpg")
        };
        var response = new ExistingUsersResponse(initialUsers)
        {
            // Act
            Users = null
        };

        // Assert
        Assert.Null(response.Users);
    }

    [Fact]
    public void Users_Property_CanBeSetToEmptyList()
    {
        // Arrange
        var initialUsers = new List<ExistingUserDto>
        {
            new("user1", true, "image1.jpg")
        };
        var response = new ExistingUsersResponse(initialUsers)
        {
            Users = []
        };
        
        // Assert
        Assert.Empty(response.Users);
    }

    [Fact]
    public void ObjectInitializer_OverridesConstructorValue()
    {
        // Arrange
        var constructorUsers = new List<ExistingUserDto>
        {
            new("constructor", true, "constructor.jpg")
        };
        var initializerUsers = new List<ExistingUserDto>
        {
            new("initializer", false, "initializer.jpg")
        };

        // Act
        var response = new ExistingUsersResponse(constructorUsers)
        {
            Users = initializerUsers
        };

        // Assert
        Assert.Equal(initializerUsers, response.Users);
        Assert.NotEqual(constructorUsers, response.Users);
    }
}