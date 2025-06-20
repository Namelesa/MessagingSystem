using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation.Dto;

public class UserInGroupDtoTests
{
    [Fact]
    public void Constructor_WithNickNameOnly_SetsNickNameAndImageIsNull()
    {
        // Arrange
        const string nickName = "TestUser";

        // Act
        var dto = new UserInGroupDto(nickName);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Null(dto.Image);
    }

    [Fact]
    public void Constructor_WithNickNameAndImage_SetsBothProperties()
    {
        // Arrange
        const string nickName = "TestUser";
        const string image = "profile.jpg";

        // Act
        var dto = new UserInGroupDto(nickName, image);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Equal(image, dto.Image);
    }

    [Fact]
    public void Constructor_WithNickNameAndNullImage_SetsNickNameAndImageIsNull()
    {
        // Arrange
        const string nickName = "TestUser";

        // Act
        var dto = new UserInGroupDto(nickName);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Null(dto.Image);
    }

    [Fact]
    public void NickName_Getter_ReturnsSetValue()
    {
        // Arrange
        const string initialNickName = "InitialUser";
        var dto = new UserInGroupDto(initialNickName);

        // Act
        var result = dto.NickName;

        // Assert
        Assert.Equal(initialNickName, result);
    }

    [Fact]
    public void NickName_Setter_UpdatesValue()
    {
        // Arrange
        const string initialNickName = "InitialUser";
        const string newNickName = "NewUser";
        var dto = new UserInGroupDto(initialNickName)
        {
            // Act
            NickName = newNickName
        };

        // Assert
        Assert.Equal(newNickName, dto.NickName);
    }

    [Fact]
    public void Image_Getter_ReturnsSetValue()
    {
        // Arrange
        const string nickName = "TestUser";
        const string image = "profile.jpg";
        var dto = new UserInGroupDto(nickName, image);

        // Act
        var result = dto.Image;

        // Assert
        Assert.Equal(image, result);
    }

    [Fact]
    public void Image_Setter_UpdatesValue()
    {
        // Arrange
        const string nickName = "TestUser";
        const string initialImage = "initial.jpg";
        const string newImage = "new.jpg";
        var dto = new UserInGroupDto(nickName, initialImage)
        {
            // Act
            Image = newImage
        };

        // Assert
        Assert.Equal(newImage, dto.Image);
    }

    [Fact]
    public void Image_Setter_CanSetToNull()
    {
        // Arrange
        const string nickName = "TestUser";
        const string initialImage = "initial.jpg";
        var dto = new UserInGroupDto(nickName, initialImage)
        {
            // Act
            Image = null
        };

        // Assert
        Assert.Null(dto.Image);
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("User123")]
    [InlineData("User With Spaces")]
    [InlineData("User")]
    public void Constructor_WithVariousNickNames_SetsNickNameCorrectly(string nickName)
    {
        // Act
        var dto = new UserInGroupDto(nickName);

        // Assert
        Assert.Equal(nickName, dto.NickName);
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("image.png")]
    [InlineData("path/to/image.jpg")]
    [InlineData("https://example.com/image.gif")]
    public void Constructor_WithVariousImages_SetsImageCorrectly(string image)
    {
        // Arrange
        const string nickName = "TestUser";

        // Act
        var dto = new UserInGroupDto(nickName, image);

        // Assert
        Assert.Equal(image, dto.Image);
    }
}