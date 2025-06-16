using MessagingSystem.Services.User.Application.User.Dto;

namespace MessagingSystem.Tests.User.UnitTests.Application.User;

public class UserFoundDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties_WithCorrectValues()
    {
        // Arrange
        const string nickName = "testNick";
        const string image = "testImage";

        // Act
        var userFoundDto = new UserFoundDto(nickName, image);

        // Assert
        Assert.Equal(nickName, userFoundDto.UserNickName);
        Assert.Equal(image, userFoundDto.Image);
    }

    [Fact]
    public void Constructor_WithNullImage_ShouldInitializeCorrectly()
    {
        // Arrange
        const string nickName = "testNick";

        // Act
        var userFoundDto = new UserFoundDto(nickName, null);

        // Assert
        Assert.Equal(nickName, userFoundDto.UserNickName);
        Assert.Null(userFoundDto.Image);
    }

    [Fact]
    public void Properties_ShouldHaveCorrectAccessors()
    {
        // Arrange
        var type = typeof(UserFoundDto);

        // Act & Assert
        var userNickNameProperty = type.GetProperty("UserNickName");
        Assert.NotNull(userNickNameProperty);
        Assert.NotNull(userNickNameProperty.GetMethod);
        Assert.NotNull(userNickNameProperty.SetMethod);

        var imageProperty = type.GetProperty("Image");
        Assert.NotNull(imageProperty);
        Assert.NotNull(imageProperty.GetMethod);
        Assert.NotNull(imageProperty.SetMethod);
    }

    [Fact]
    public void Properties_CanBeModified_AfterInitialization()
    {
        // Arrange
        var userFoundDto = new UserFoundDto("initialNick", "initialImage")
        {
            // Act
            UserNickName = "newNick",
            Image = "newImage"
        };

        // Assert
        Assert.Equal("newNick", userFoundDto.UserNickName);
        Assert.Equal("newImage", userFoundDto.Image);
    }

    [Fact]
    public void Image_CanBeSetToNull_AfterInitialization()
    {
        // Arrange
        var userFoundDto = new UserFoundDto("testNick", "initialImage")
        {
            // Act
            Image = null
        };

        // Assert
        Assert.Equal("testNick", userFoundDto.UserNickName);
        Assert.Null(userFoundDto.Image);
    }

    [Fact]
    public void Object_Initialization_ShouldWork()
    {
        // Arrange & Act
        var userFoundDto = new UserFoundDto("initialNick", "initialImage")
        {
            UserNickName = "newNick",
            Image = "newImage"
        };

        // Assert
        Assert.Equal("newNick", userFoundDto.UserNickName);
        Assert.Equal("newImage", userFoundDto.Image);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_ShouldNotThrowException()
    {
        // Act & Assert
        var exception = Record.Exception(() => new UserFoundDto("", ""));
        Assert.Null(exception);
    }
}