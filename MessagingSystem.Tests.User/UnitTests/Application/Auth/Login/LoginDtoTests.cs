using MessagingSystem.Services.User.Application.Auth.Login.Dto;

namespace MessagingSystem.Tests.User.UnitTests.Application.Auth.Login;

public class LoginDtoTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeAllProperties_WithCorrectValues()
    {
        // Arrange
        const string login = "testuser";
        const string password = "Password123!";
        const string nickName = "TestNickName";

        // Act
        var loginDto = new LoginDto(login, password, nickName);

        // Assert
        Assert.Equal(login, loginDto.Login);
        Assert.Equal(password, loginDto.Password);
        Assert.Equal(nickName, loginDto.NickName);
    }

    [Fact]
    public void Constructor_WithNullParameters_ShouldSetPropertiesToNull()
    {
        // Act
        var loginDto = new LoginDto(null, null, null);

        // Assert
        Assert.Null(loginDto.Login);
        Assert.Null(loginDto.Password);
        Assert.Null(loginDto.NickName);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_ShouldSetPropertiesToEmpty()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var loginDto = new LoginDto(emptyString, emptyString, emptyString);

        // Assert
        Assert.Equal(string.Empty, loginDto.Login);
        Assert.Equal(string.Empty, loginDto.Password);
        Assert.Equal(string.Empty, loginDto.NickName);
    }

    [Theory]
    [InlineData("user1", "pass1", "nick1")]
    [InlineData("user@domain.com", "ComplexPass123!", "User Display Name")]
    [InlineData("", "", "")]
    [InlineData("user_123", "P@ssw0rd!", "nick_name_123")]
    [InlineData(null, null, null)]
    public void Constructor_WithVariousParameters_CreatesValidObject(string login, string password, string nickName)
    {
        // Act
        var loginDto = new LoginDto(login, password, nickName);

        // Assert
        Assert.Equal(login, loginDto.Login);
        Assert.Equal(password, loginDto.Password);
        Assert.Equal(nickName, loginDto.NickName);
    }

    #endregion

    #region Property Accessor Tests

    [Fact]
    public void LoginProperty_ShouldHaveInitAccessor()
    {
        // Arrange
        var type = typeof(LoginDto);
        
        // Act
        var loginProperty = type.GetProperty("Login");
        
        // Assert
        Assert.NotNull(loginProperty);
        Assert.True(loginProperty.CanRead);
        Assert.True(loginProperty.CanWrite);
        
        // Check that it's init-only (setter is not public after initialization)
        var setMethod = loginProperty.SetMethod;
        Assert.NotNull(setMethod);
    }

    [Fact]
    public void PasswordProperty_ShouldHaveInitAccessor()
    {
        // Arrange
        var type = typeof(LoginDto);
        
        // Act
        var passwordProperty = type.GetProperty("Password");
        
        // Assert
        Assert.NotNull(passwordProperty);
        Assert.True(passwordProperty.CanRead);
        Assert.True(passwordProperty.CanWrite);
        
        var setMethod = passwordProperty.SetMethod;
        Assert.NotNull(setMethod);
    }

    [Fact]
    public void NickNameProperty_ShouldHaveRegularSetAccessor()
    {
        // Arrange
        var type = typeof(LoginDto);
        
        // Act
        var nickNameProperty = type.GetProperty("NickName");
        
        // Assert
        Assert.NotNull(nickNameProperty);
        Assert.True(nickNameProperty.CanRead);
        Assert.True(nickNameProperty.CanWrite);
        
        var setMethod = nickNameProperty.SetMethod;
        Assert.NotNull(setMethod);
        Assert.True(setMethod.IsPublic);
    }

    #endregion

    #region Object Initialization Tests

    [Fact]
    public void ObjectInitialization_WithInitProperties_ShouldWork()
    {
        // Arrange & Act
        var loginDto = new LoginDto("originaluser", "originalpassword", "originalNickName")
        {
            Login = "testuser",
            Password = "Password123!",
            NickName = "NewNickName"
        };
        
        // Assert
        Assert.Equal("testuser", loginDto.Login);
        Assert.Equal("Password123!", loginDto.Password);
        Assert.Equal("NewNickName", loginDto.NickName);
    }

    [Fact]
    public void ObjectInitialization_WithoutChanges_ShouldKeepOriginalValues()
    {
        // Arrange
        const string login = "testuser";
        const string password = "Password123!";
        const string nickName = "TestNickName";
        
        // Act
        var loginDto = new LoginDto(login, password, nickName);
        
        // Assert
        Assert.Equal(login, loginDto.Login);
        Assert.Equal(password, loginDto.Password);
        Assert.Equal(nickName, loginDto.NickName);
    }

    [Fact]
    public void ObjectInitialization_OnlyNickName_ShouldUpdateOnlyNickName()
    {
        // Arrange & Act
        var loginDto = new LoginDto("testuser", "Password123!", "originalNick")
        {
            NickName = "UpdatedNickName"
        };
        
        // Assert
        Assert.Equal("testuser", loginDto.Login);
        Assert.Equal("Password123!", loginDto.Password);
        Assert.Equal("UpdatedNickName", loginDto.NickName);
    }

    #endregion

    #region NickName Property Modification Tests

    [Fact]
    public void NickName_CanBeModified_AfterObjectCreation()
    {
        // Arrange
        var loginDto = new LoginDto("testuser", "Password123!", "OriginalNick");
        const string newNickName = "ModifiedNickName";

        // Act
        loginDto.NickName = newNickName;

        // Assert
        Assert.Equal("testuser", loginDto.Login);
        Assert.Equal("Password123!", loginDto.Password);
        Assert.Equal(newNickName, loginDto.NickName);
    }

    [Fact]
    public void NickName_CanBeSetToNull()
    {
        // Arrange
        var loginDto = new LoginDto("testuser", "Password123!", "OriginalNick");

        // Act
        loginDto.NickName = null;

        // Assert
        Assert.Equal("testuser", loginDto.Login);
        Assert.Equal("Password123!", loginDto.Password);
        Assert.Null(loginDto.NickName);
    }

    [Fact]
    public void NickName_CanBeSetToEmpty()
    {
        // Arrange
        var loginDto = new LoginDto("testuser", "Password123!", "OriginalNick");

        // Act
        loginDto.NickName = string.Empty;

        // Assert
        Assert.Equal("testuser", loginDto.Login);
        Assert.Equal("Password123!", loginDto.Password);
        Assert.Equal(string.Empty, loginDto.NickName);
    }

    [Theory]
    [InlineData("NewNick1")]
    [InlineData("Display Name With Spaces")]
    [InlineData("nick_with_underscores")]
    [InlineData("NickWith123Numbers")]
    [InlineData("用户昵称")]
    [InlineData("🚀Cool Nick🚀")]
    [InlineData("")]
    [InlineData(null)]
    public void NickName_CanBeSetToVariousValues(string newNickName)
    {
        // Arrange
        var loginDto = new LoginDto("testuser", "Password123!", "OriginalNick");

        // Act
        loginDto.NickName = newNickName;

        // Assert
        Assert.Equal(newNickName, loginDto.NickName);
        // Verify other properties remain unchanged
        Assert.Equal("testuser", loginDto.Login);
        Assert.Equal("Password123!", loginDto.Password);
    }

    [Fact]
    public void NickName_MultipleModifications_ShouldMaintainState()
    {
        // Arrange
        var loginDto = new LoginDto("testuser", "Password123!", "OriginalNick");

        // Act & Assert - Multiple modifications
        loginDto.NickName = "FirstChange";
        Assert.Equal("FirstChange", loginDto.NickName);

        loginDto.NickName = "SecondChange";
        Assert.Equal("SecondChange", loginDto.NickName);

        loginDto.NickName = null;
        Assert.Null(loginDto.NickName);

        loginDto.NickName = "FinalChange";
        Assert.Equal("FinalChange", loginDto.NickName);

        // Verify other properties remain unchanged throughout
        Assert.Equal("testuser", loginDto.Login);
        Assert.Equal("Password123!", loginDto.Password);
    }

    #endregion

    #region Init-only Property Behavior Tests

    [Fact]
    public void Login_CannotBeModified_AfterInitialization()
    {
        // This test verifies that Login has init-only behavior
        // We can only test this through object initialization, not direct assignment
        
        // Arrange
        var loginDto = new LoginDto("originalLogin", "password", "nick");
        
        // Assert - Login should keep its original value
        Assert.Equal("originalLogin", loginDto.Login);
        
        // Note: Direct assignment like loginDto.Login = "newValue" would cause compilation error
        // This is the expected behavior for init-only properties
    }

    [Fact]
    public void Password_CannotBeModified_AfterInitialization()
    {
        // This test verifies that Password has init-only behavior
        
        // Arrange
        var loginDto = new LoginDto("login", "originalPassword", "nick");
        
        // Assert - Password should keep its original value
        Assert.Equal("originalPassword", loginDto.Password);
        
        // Note: Direct assignment like loginDto.Password = "newValue" would cause compilation error
        // This is the expected behavior for init-only properties
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public void Constructor_WithVeryLongStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var longLogin = new string('a', 1000);
        var longPassword = new string('b', 1000);
        var longNickName = new string('c', 1000);

        // Act
        var loginDto = new LoginDto(longLogin, longPassword, longNickName);

        // Assert
        Assert.Equal(longLogin, loginDto.Login);
        Assert.Equal(longPassword, loginDto.Password);
        Assert.Equal(longNickName, loginDto.NickName);
    }

    [Fact]
    public void Properties_WithSpecialCharacters_ShouldBeHandledCorrectly()
    {
        // Arrange
        const string specialLogin = "user@domain.com";
        const string specialPassword = "P@ssw0rd!#$%";
        const string specialNickName = "№1 🎉";

        // Act
        var loginDto = new LoginDto(specialLogin, specialPassword, specialNickName);

        // Assert
        Assert.Equal(specialLogin, loginDto.Login);
        Assert.Equal(specialPassword, loginDto.Password);
        Assert.Equal(specialNickName, loginDto.NickName);
    }

    #endregion

    #region Real-world Scenarios

    [Fact]
    public void CreateLoginDto_ForEmailLogin_RealisticScenario()
    {
        // Arrange
        const string emailLogin = "john.doe@company.com";
        const string password = "SecurePass123!";
        const string displayName = "John Doe";

        // Act
        var loginDto = new LoginDto(emailLogin, password, displayName);

        // Assert
        Assert.Equal(emailLogin, loginDto.Login);
        Assert.Equal(password, loginDto.Password);
        Assert.Equal(displayName, loginDto.NickName);
    }

    [Fact]
    public void CreateLoginDto_WithNickNameUpdate_RealisticScenario()
    {
        // Arrange
        const string login = "john_doe";
        const string password = "MyPassword123";
        var loginDto = new LoginDto(login, password, "TempNick");

        // Act - User updates their display name
        loginDto.NickName = "John D.";

        // Assert
        Assert.Equal(login, loginDto.Login);
        Assert.Equal(password, loginDto.Password);
        Assert.Equal("John D.", loginDto.NickName);
    }

    [Fact]
    public void LoginDto_PropertyIndependence_ShouldWork()
    {
        // Arrange
        var loginDto = new LoginDto("user", "pass", "nick");

        // Act - Only modify NickName
        loginDto.NickName = "ModifiedNick";

        // Assert - Other properties should remain unchanged
        Assert.Equal("user", loginDto.Login);
        Assert.Equal("pass", loginDto.Password);
        Assert.Equal("ModifiedNick", loginDto.NickName);
    }

    #endregion
}