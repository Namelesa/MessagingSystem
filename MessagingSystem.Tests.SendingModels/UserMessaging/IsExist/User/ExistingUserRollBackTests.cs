using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.IsExist.User;

public class ExistingUserRollBackTests
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
        var response = new ExistingUserResponse(expectedNickName, expectedIsExist, expectedImage);

        // Assert
        Assert.Equal(expectedNickName, response.NickName);
        Assert.Equal(expectedIsExist, response.IsExist);
        Assert.Equal(expectedImage, response.Image);
    }

    [Fact]
    public void Constructor_WithNullNickName_SetsNickNameToNull()
    {
        // Arrange
        const bool isExist = true;
        const string image = "test.jpg";

        // Act
        var response = new ExistingUserResponse(null, isExist, image);

        // Assert
        Assert.Null(response.NickName);
        Assert.Equal(isExist, response.IsExist);
        Assert.Equal(image, response.Image);
    }

    [Fact]
    public void Constructor_WithEmptyNickName_SetsNickNameToEmpty()
    {
        // Arrange
        var emptyNickName = string.Empty;
        const bool isExist = false;
        const string image = "default.png";

        // Act
        var response = new ExistingUserResponse(emptyNickName, isExist, image);

        // Assert
        Assert.Equal(string.Empty, response.NickName);
        Assert.Equal(isExist, response.IsExist);
        Assert.Equal(image, response.Image);
    }

    [Fact]
    public void Constructor_WithNullImage_SetsImageToNull()
    {
        // Arrange
        const string nickName = "user123";
        const bool isExist = true;

        // Act
        var response = new ExistingUserResponse(nickName, isExist, null);

        // Assert
        Assert.Equal(nickName, response.NickName);
        Assert.Equal(isExist, response.IsExist);
        Assert.Null(response.Image);
    }

    [Fact]
    public void Constructor_WithEmptyImage_SetsImageToEmpty()
    {
        // Arrange
        const string nickName = "user123";
        const bool isExist = false;
        var emptyImage = string.Empty;

        // Act
        var response = new ExistingUserResponse(nickName, isExist, emptyImage);

        // Assert
        Assert.Equal(nickName, response.NickName);
        Assert.Equal(isExist, response.IsExist);
        Assert.Equal(string.Empty, response.Image);
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
        var response = new ExistingUserResponse(nickName, isExist, image);

        // Assert
        Assert.Equal(nickName, response.NickName);
        Assert.Equal(isExist, response.IsExist);
        Assert.Equal(image, response.Image);
    }

    #endregion

    #region Property Modification Tests

    [Fact]
    public void NickName_CanBeModified()
    {
        // Arrange
        var response = new ExistingUserResponse("initial_nick", true, "image.jpg");
        const string newNickName = "modified_nick";

        // Act
        response.NickName = newNickName;

        // Assert
        Assert.Equal(newNickName, response.NickName);
    }

    [Fact]
    public void NickName_CanBeSetToNull()
    {
        // Arrange
        var response = new ExistingUserResponse("initial_nick", true, "image.jpg")
        {
            // Act
            NickName = null
        };

        // Assert
        Assert.Null(response.NickName);
    }

    [Fact]
    public void NickName_CanBeSetToEmpty()
    {
        // Arrange
        var response = new ExistingUserResponse("initial_nick", true, "image.jpg")
        {
            // Act
            NickName = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, response.NickName);
    }

    [Fact]
    public void Image_CanBeModified()
    {
        // Arrange
        var response = new ExistingUserResponse("user", true, "initial.jpg");
        const string newImage = "modified.png";

        // Act
        response.Image = newImage;

        // Assert
        Assert.Equal(newImage, response.Image);
    }

    [Fact]
    public void Image_CanBeSetToNull()
    {
        // Arrange
        var response = new ExistingUserResponse("user", true, "initial.jpg")
        {
            // Act
            Image = null
        };

        // Assert
        Assert.Null(response.Image);
    }

    [Fact]
    public void Image_CanBeSetToEmpty()
    {
        // Arrange
        var response = new ExistingUserResponse("user", true, "initial.jpg")
        {
            // Act
            Image = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, response.Image);
    }

    [Fact]
    public void IsExist_CanBeModified()
    {
        // Arrange
        var response = new ExistingUserResponse("user", true, "image.jpg")
        {
            // Act
            IsExist = false
        };

        // Assert
        Assert.False(response.IsExist);
    }

    [Xunit.Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsExist_CanBeSetToBothValues(bool value)
    {
        // Arrange
        var response = new ExistingUserResponse("user", !value, "image.jpg")
        {
            // Act
            IsExist = value
        };

        // Assert
        Assert.Equal(value, response.IsExist);
    }

    #endregion

    #region Properties Independence Tests

    [Fact]
    public void Properties_AreIndependentlyModifiable()
    {
        // Arrange
        var response = new ExistingUserResponse("initial", true, "initial.jpg");
        const string newNickName = "modified_nick";
        const string newImage = "modified.png";
        const bool newIsExist = false;

        // Act
        response.NickName = newNickName;
        response.Image = newImage;
        response.IsExist = newIsExist;

        // Assert
        Assert.Equal(newNickName, response.NickName);
        Assert.Equal(newImage, response.Image);
        Assert.Equal(newIsExist, response.IsExist);
    }

    [Fact]
    public void MultipleModifications_MaintainState()
    {
        // Arrange
        var response = new ExistingUserResponse("user1", true, "image1.jpg")
        {
            // Act & Assert - Multiple state changes
            NickName = "user2",
            IsExist = false,
            Image = "image2.png"
        };

        Assert.Equal("user2", response.NickName);
        Assert.False(response.IsExist);
        Assert.Equal("image2.png", response.Image);

        response.NickName = null;
        response.IsExist = true;
        response.Image = null;
        Assert.Null(response.NickName);
        Assert.True(response.IsExist);
        Assert.Null(response.Image);

        response.NickName = string.Empty;
        response.Image = string.Empty;
        Assert.Equal(string.Empty, response.NickName);
        Assert.True(response.IsExist);
        Assert.Equal(string.Empty, response.Image);
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
        var response = new ExistingUserResponse(specialNickName, true, "image.jpg");

        // Assert
        Assert.Equal(specialNickName, response.NickName);
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
        var response = new ExistingUserResponse("user", true, imageValue);

        // Assert
        Assert.Equal(imageValue, response.Image);
    }

    [Fact]
    public void Constructor_WithVeryLongStrings_HandlesCorrectly()
    {
        // Arrange
        var longNickName = new string('a', 1000);
        var longImage = new string('b', 2000);

        // Act
        var response = new ExistingUserResponse(longNickName, true, longImage);

        // Assert
        Assert.Equal(longNickName, response.NickName);
        Assert.Equal(longImage, response.Image);
        Assert.True(response.IsExist);
    }

    #endregion

    #region Real-world Scenarios Tests

    [Fact]
    public void CreateExistingUserResponse_RealisticScenario()
    {
        // Arrange
        const string nickName = "john.doe@company.com";
        const string image = "https://cdn.example.com/avatars/johndoe.jpg";
        const bool isExist = true;

        // Act
        var response = new ExistingUserResponse(nickName, isExist, image);

        // Assert
        Assert.Equal(nickName, response.NickName);
        Assert.Equal(image, response.Image);
        Assert.True(response.IsExist);
    }

    [Fact]
    public void CreateNonExistingUserResponse_RealisticScenario()
    {
        // Arrange
        const string nickName = "unknown_user";
        const bool isExist = false;
        string? image = null; // No image for non-existing user

        // Act
        var response = new ExistingUserResponse(nickName, isExist, image);

        // Assert
        Assert.Equal(nickName, response.NickName);
        Assert.Null(response.Image);
        Assert.False(response.IsExist);
    }

    [Fact]
    public void UpdateResponseData_AfterUserCreation()
    {
        // Arrange
        var response = new ExistingUserResponse("new_user", false, null)
        {
            // Act - User gets created and gets an image
            IsExist = true,
            Image = "profile_pic.jpg"
        };

        // Assert
        Assert.Equal("new_user", response.NickName);
        Assert.True(response.IsExist);
        Assert.Equal("profile_pic.jpg", response.Image);
    }

    #endregion
}