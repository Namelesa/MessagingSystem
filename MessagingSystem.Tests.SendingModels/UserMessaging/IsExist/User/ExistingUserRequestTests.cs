using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.IsExist.User;

public class ExistingUserRequestTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidNickName_SetsPropertyCorrectly()
    {
        // Arrange
        const string expectedNickName = "testUser";

        // Act
        var request = new ExistingUserRequest(expectedNickName);

        // Assert
        Assert.Equal(expectedNickName, request.NickName);
    }

    [Fact]
    public void Constructor_WithNullNickName_SetsPropertyToNull()
    {
        // Arrange
        string? nullNickName = null;

        // Act
        var request = new ExistingUserRequest(nullNickName);

        // Assert
        Assert.Null(request.NickName);
    }

    [Fact]
    public void Constructor_WithEmptyNickName_SetsPropertyToEmptyString()
    {
        // Arrange
        var emptyNickName = string.Empty;

        // Act
        var request = new ExistingUserRequest(emptyNickName);

        // Assert
        Assert.Equal(string.Empty, request.NickName);
    }

    [Xunit.Theory]
    [InlineData("john_doe")]
    [InlineData("jane.smith@company.com")]
    [InlineData("")]
    [InlineData("user123")]
    [InlineData("user_with_special_chars!@#")]
    [InlineData("user with spaces")]
    [InlineData("用户名")]
    [InlineData("🚀user🚀")]
    [InlineData(null)]
    public void Constructor_WithVariousNickNames_CreatesValidObject(string nickName)
    {
        // Act
        var request = new ExistingUserRequest(nickName);

        // Assert
        Assert.Equal(nickName, request.NickName);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void NickName_IsInitOnly_CannotBeModifiedAfterCreation()
    {
        // Arrange
        const string initialNickName = "initial_user";
        var request = new ExistingUserRequest(initialNickName);

        // Assert
        Assert.Equal(initialNickName, request.NickName);
    }

    [Fact]
    public void NickName_CanBeSetDuringObjectInitialization()
    {
        // Arrange
        const string expectedNickName = "initialized_name";

        // Act
        var request = new ExistingUserRequest("temp") { NickName = expectedNickName };

        // Assert
        Assert.Equal(expectedNickName, request.NickName);
    }

    #endregion

    #region Edge Cases Tests

    [Xunit.Theory]
    [InlineData("user@domain.com")]
    [InlineData("user.with.dots")]
    [InlineData("user-with-dashes")]
    [InlineData("user_with_underscores")]
    [InlineData("123456789")]
    [InlineData("user with spaces")]
    [InlineData("用户名")]
    [InlineData("🚀user🚀")]
    public void Constructor_WithSpecialCharacters_HandlesCorrectly(string specialNickName)
    {
        // Act
        var request = new ExistingUserRequest(specialNickName);

        // Assert
        Assert.Equal(specialNickName, request.NickName);
    }

    [Fact]
    public void Constructor_WithVeryLongNickName_HandlesCorrectly()
    {
        // Arrange
        var longNickName = new string('a', 1000);

        // Act
        var request = new ExistingUserRequest(longNickName);

        // Assert
        Assert.Equal(longNickName, request.NickName);
    }

    #endregion

    #region Real-world Scenarios Tests

    [Fact]
    public void CreateRequest_ForEmailBasedNickName_RealisticScenario()
    {
        // Arrange
        const string emailNickName = "john.doe@company.com";

        // Act
        var request = new ExistingUserRequest(emailNickName);

        // Assert
        Assert.Equal(emailNickName, request.NickName);
    }

    [Fact]
    public void CreateRequest_ForUsernameBasedNickName_RealisticScenario()
    {
        // Arrange
        const string username = "john_doe_123";

        // Act
        var request = new ExistingUserRequest(username);

        // Assert
        Assert.Equal(username, request.NickName);
    }

    #endregion
}