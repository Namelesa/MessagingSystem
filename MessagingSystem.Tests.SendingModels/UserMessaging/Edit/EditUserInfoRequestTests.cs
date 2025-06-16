using MessagingSystem.SendingModels.UserMessaging.Edit;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.Edit;

public class EditUserInfoRequestTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var expectedUserHash = "hash123";
        var expectedUserNickName = "testuser";
        var expectedImage = "image.jpg";

        // Act
        var request = new EditUserInfoRequest(expectedUserHash, expectedUserNickName, expectedImage);

        // Assert
        Assert.Equal(expectedUserHash, request.UserHash);
        Assert.Equal(expectedUserNickName, request.UserNickName);
        Assert.Equal(expectedImage, request.Image);
    }

    [Fact]
    public void Constructor_WithNullValues_SetsPropertiesToNull()
    {
        // Arrange
        string? nullUserHash = null;
        string? nullUserNickName = null;
        string? nullImage = null;

        // Act
        var request = new EditUserInfoRequest(nullUserHash, nullUserNickName, nullImage);

        // Assert
        Assert.Null(request.UserHash);
        Assert.Null(request.UserNickName);
        Assert.Null(request.Image);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_SetsPropertiesToEmptyStrings()
    {
        // Arrange
        var emptyUserHash = string.Empty;
        var emptyUserNickName = string.Empty;
        var emptyImage = string.Empty;

        // Act
        var request = new EditUserInfoRequest(emptyUserHash, emptyUserNickName, emptyImage);

        // Assert
        Assert.Equal(string.Empty, request.UserHash);
        Assert.Equal(string.Empty, request.UserNickName);
        Assert.Equal(string.Empty, request.Image);
    }

    [Fact]
    public void UserHash_Property_CanBeModified()
    {
        // Arrange
        var initialHash = "initial_hash";
        var newHash = "new_hash";
        var request = new EditUserInfoRequest(initialHash, "nick", "image")
        {
            // Act
            UserHash = newHash
        };

        // Assert
        Assert.Equal(newHash, request.UserHash);
    }

    [Fact]
    public void UserNickName_Property_CanBeModified()
    {
        // Arrange
        var initialNickName = "initial_nick";
        var newNickName = "new_nick";
        var request = new EditUserInfoRequest("hash", initialNickName, "image")
        {
            // Act
            UserNickName = newNickName
        };

        // Assert
        Assert.Equal(newNickName, request.UserNickName);
    }

    [Fact]
    public void Image_Property_CanBeModified()
    {
        // Arrange
        var initialImage = "initial.jpg";
        var newImage = "new.png";
        var request = new EditUserInfoRequest("hash", "nick", initialImage)
        {
            // Act
            Image = newImage
        };

        // Assert
        Assert.Equal(newImage, request.Image);
    }

    [Fact]
    public void Properties_CanBeSetToNull()
    {
        // Arrange
        var request = new EditUserInfoRequest("hash", "nick", "image")
        {
            // Act
            UserHash = null,
            UserNickName = null,
            Image = null
        };

        // Assert
        Assert.Null(request.UserHash);
        Assert.Null(request.UserNickName);
        Assert.Null(request.Image);
    }

    [Fact]
    public void Properties_CanBeSetToEmptyString()
    {
        // Arrange
        var request = new EditUserInfoRequest("hash", "nick", "image")
        {
            // Act
            UserHash = string.Empty,
            UserNickName = string.Empty,
            Image = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, request.UserHash);
        Assert.Equal(string.Empty, request.UserNickName);
        Assert.Equal(string.Empty, request.Image);
    }

    [Xunit.Theory]
    [InlineData("hash1", "user1", "image1.jpg")]
    [InlineData("", "", "")]
    [InlineData(null, null, null)]
    [InlineData("special!@#$%", "nick_with_underscore", "file with spaces.png")]
    public void Constructor_WithVariousParameters_CreatesValidObject(string userHash, string userNickName, string image)
    {
        // Act
        var request = new EditUserInfoRequest(userHash, userNickName, image);

        // Assert
        Assert.Equal(userHash, request.UserHash);
        Assert.Equal(userNickName, request.UserNickName);
        Assert.Equal(image, request.Image);
    }

    [Fact]
    public void ObjectInitializer_OverridesConstructorValues()
    {
        // Arrange
        var constructorHash = "constructor_hash";
        var constructorNick = "constructor_nick";
        var constructorImage = "constructor_image";
        var initializerHash = "initializer_hash";
        var initializerNick = "initializer_nick";
        var initializerImage = "initializer_image";

        // Act
        var request = new EditUserInfoRequest(constructorHash, constructorNick, constructorImage)
        {
            UserHash = initializerHash,
            UserNickName = initializerNick,
            Image = initializerImage
        };

        // Assert
        Assert.Equal(initializerHash, request.UserHash);
        Assert.Equal(initializerNick, request.UserNickName);
        Assert.Equal(initializerImage, request.Image);
    }
}