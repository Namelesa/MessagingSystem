using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.IsExist.Users;

public class ExistingUserDtoTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_SetsAllPropertiesCorrectly()
    {
        // Arrange
        const string expectedNickName = "testUser";
        const bool expectedIsExist = true;
        const string expectedImage = "user-avatar.jpg";

        // Act
        var dto = new ExistingUserDto(expectedNickName, expectedIsExist, expectedImage);

        // Assert
        Assert.Equal(expectedNickName, dto.NickName);
        Assert.Equal(expectedIsExist, dto.IsExist);
        Assert.Equal(expectedImage, dto.Image);
    }

    [Fact]
    public void Constructor_WithNullNickName_SetsNickNameToNull()
    {
        // Arrange
        const bool isExist = true;
        const string image = "test.jpg";

        // Act
        var dto = new ExistingUserDto(null, isExist, image);

        // Assert
        Assert.Null(dto.NickName);
        Assert.Equal(isExist, dto.IsExist);
        Assert.Equal(image, dto.Image);
    }

    [Fact]
    public void Constructor_WithEmptyNickName_SetsNickNameToEmpty()
    {
        // Arrange
        var emptyNickName = string.Empty;
        const bool isExist = false;
        const string image = "default.png";

        // Act
        var dto = new ExistingUserDto(emptyNickName, isExist, image);

        // Assert
        Assert.Equal(string.Empty, dto.NickName);
        Assert.Equal(isExist, dto.IsExist);
        Assert.Equal(image, dto.Image);
    }

    [Fact]
    public void Constructor_WithNullImage_SetsImageToNull()
    {
        // Arrange
        const string nickName = "user123";
        const bool isExist = true;

        // Act
        var dto = new ExistingUserDto(nickName, isExist, null);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Equal(isExist, dto.IsExist);
        Assert.Null(dto.Image);
    }

    [Fact]
    public void Constructor_WithEmptyImage_SetsImageToEmpty()
    {
        // Arrange
        const string nickName = "user123";
        const bool isExist = false;
        var emptyImage = string.Empty;

        // Act
        var dto = new ExistingUserDto(nickName, isExist, emptyImage);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Equal(isExist, dto.IsExist);
        Assert.Equal(string.Empty, dto.Image);
    }

    [Xunit.Theory]
    [InlineData("john_doe", true, "avatar1.jpg")]
    [InlineData("jane_smith", false, "avatar2.png")]
    [InlineData("", true, "")]
    [InlineData("user@domain.com", false, "profile.gif")]
    [InlineData("user", true, "фото.jpg")]
    [InlineData("user with spaces", false, "image with spaces.png")]
    [InlineData(null, true, null)]
    [InlineData(null, false, "")]
    [InlineData("", false, null)]
    public void Constructor_WithVariousParameters_CreatesValidObject(string nickName, bool isExist, string image)
    {
        // Act
        var dto = new ExistingUserDto(nickName, isExist, image);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Equal(isExist, dto.IsExist);
        Assert.Equal(image, dto.Image);
    }

    #endregion

    #region Property Modification Tests

    [Fact]
    public void NickName_CanBeModified()
    {
        // Arrange
        var dto = new ExistingUserDto("initial_nick", true, "image.jpg");
        const string newNickName = "modified_nick";

        // Act
        dto.NickName = newNickName;

        // Assert
        Assert.Equal(newNickName, dto.NickName);
    }

    [Fact]
    public void NickName_CanBeSetToNull()
    {
        // Arrange
        var dto = new ExistingUserDto("initial_nick", true, "image.jpg")
        {
            // Act
            NickName = null
        };

        // Assert
        Assert.Null(dto.NickName);
    }

    [Fact]
    public void NickName_CanBeSetToEmpty()
    {
        // Arrange
        var dto = new ExistingUserDto("initial_nick", true, "image.jpg")
        {
            // Act
            NickName = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, dto.NickName);
    }

    [Fact]
    public void Image_CanBeModified()
    {
        // Arrange
        var dto = new ExistingUserDto("user", true, "initial.jpg");
        const string newImage = "modified.png";

        // Act
        dto.Image = newImage;

        // Assert
        Assert.Equal(newImage, dto.Image);
    }

    [Fact]
    public void Image_CanBeSetToNull()
    {
        // Arrange
        var dto = new ExistingUserDto("user", true, "initial.jpg")
        {
            // Act
            Image = null
        };

        // Assert
        Assert.Null(dto.Image);
    }

    [Fact]
    public void Image_CanBeSetToEmpty()
    {
        // Arrange
        var dto = new ExistingUserDto("user", true, "initial.jpg")
        {
            // Act
            Image = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, dto.Image);
    }

    [Fact]
    public void IsExist_CanBeModified()
    {
        // Arrange
        var dto = new ExistingUserDto("user", true, "image.jpg")
        {
            // Act
            IsExist = false
        };

        // Assert
        Assert.False(dto.IsExist);
    }

    [Xunit.Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsExist_CanBeSetToBothValues(bool value)
    {
        // Arrange
        var dto = new ExistingUserDto("user", !value, "image.jpg")
        {
            // Act
            IsExist = value
        };

        // Assert
        Assert.Equal(value, dto.IsExist);
    }

    #endregion

    #region Properties Independence Tests

    [Fact]
    public void Properties_AreIndependentlyModifiable()
    {
        // Arrange
        var dto = new ExistingUserDto("initial", true, "initial.jpg");
        const string newNickName = "modified_nick";
        const string newImage = "modified.png";
        const bool newIsExist = false;

        // Act
        dto.NickName = newNickName;
        dto.Image = newImage;
        dto.IsExist = newIsExist;

        // Assert
        Assert.Equal(newNickName, dto.NickName);
        Assert.Equal(newImage, dto.Image);
        Assert.Equal(newIsExist, dto.IsExist);
    }

    [Fact]
    public void MultipleModifications_MaintainState()
    {
        // Arrange
        var dto = new ExistingUserDto("user1", true, "image1.jpg")
        {
            // Act & Assert - Multiple state changes
            NickName = "user2",
            IsExist = false,
            Image = "image2.png"
        };

        Assert.Equal("user2", dto.NickName);
        Assert.False(dto.IsExist);
        Assert.Equal("image2.png", dto.Image);

        dto.NickName = null;
        dto.IsExist = true;
        dto.Image = null;
        Assert.Null(dto.NickName);
        Assert.True(dto.IsExist);
        Assert.Null(dto.Image);

        dto.NickName = string.Empty;
        dto.Image = string.Empty;
        Assert.Equal(string.Empty, dto.NickName);
        Assert.True(dto.IsExist);
        Assert.Equal(string.Empty, dto.Image);
    }

    #endregion

    #region Edge Cases and Special Characters Tests

    [Xunit.Theory]
    [InlineData("user@domain.com")]
    [InlineData("user.with.dots")]
    [InlineData("user-with-dashes")]
    [InlineData("user_with_underscores")]
    [InlineData("123456789")]
    [InlineData("MyUser")]
    [InlineData("用户名")]
    [InlineData("🚀user🚀")]
    public void NickName_WithSpecialCharacters_HandledCorrectly(string specialNickName)
    {
        // Act
        var dto = new ExistingUserDto(specialNickName, true, "image.jpg");

        // Assert
        Assert.Equal(specialNickName, dto.NickName);
    }

    [Xunit.Theory]
    [InlineData("http://example.com/image.jpg")]
    [InlineData("https://cdn.example.com/avatars/user123.png")]
    [InlineData("data:image/png;base64,iVBORw0KGgoAAAANSU")]
    [InlineData("/local/path/to/image.gif")]
    [InlineData("C:\\Windows\\image.bmp")]
    [InlineData("picture.jpg")]
    [InlineData("image with spaces.png")]
    public void Image_WithVariousFormats_HandledCorrectly(string imageValue)
    {
        // Act
        var dto = new ExistingUserDto("user", true, imageValue);

        // Assert
        Assert.Equal(imageValue, dto.Image);
    }

    [Fact]
    public void Constructor_WithVeryLongStrings_HandlesCorrectly()
    {
        // Arrange
        var longNickName = new string('a', 1000);
        var longImage = new string('b', 2000);

        // Act
        var dto = new ExistingUserDto(longNickName, true, longImage);

        // Assert
        Assert.Equal(longNickName, dto.NickName);
        Assert.Equal(longImage, dto.Image);
        Assert.True(dto.IsExist);
    }

    #endregion

    #region Real-world Scenarios Tests

    [Fact]
    public void CreateExistingUser_RealisticScenario()
    {
        // Arrange
        const string nickName = "john.doe@company.com";
        const string image = "https://cdn.example.com/avatars/johndoe.jpg";
        const bool isExist = true;

        // Act
        var dto = new ExistingUserDto(nickName, isExist, image);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Equal(image, dto.Image);
        Assert.True(dto.IsExist);
    }

    [Fact]
    public void CreateNonExistingUser_RealisticScenario()
    {
        // Arrange
        const string nickName = "unknown_user";
        const bool isExist = false;
        string? image = null; // No image for non-existing user

        // Act
        var dto = new ExistingUserDto(nickName, isExist, image);

        // Assert
        Assert.Equal(nickName, dto.NickName);
        Assert.Null(dto.Image);
        Assert.False(dto.IsExist);
    }

    [Fact]
    public void UpdateUserStatus_FromNonExistingToExisting()
    {
        // Arrange
        var dto = new ExistingUserDto("new_user", false, null)
        {
            // Act - User gets created and gets an image
            IsExist = true,
            Image = "profile_pic.jpg"
        };

        // Assert
        Assert.Equal("new_user", dto.NickName);
        Assert.True(dto.IsExist);
        Assert.Equal("profile_pic.jpg", dto.Image);
    }

    #endregion
}