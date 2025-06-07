using MessagingSystem.Services.User.Application.Auth.Register.Dto;

namespace MessagingSystem.Tests.User.UnitTests.Application.Auth.Register;

public class RegisterDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties_WithCorrectValues()
    {
        // Arrange
        const string email = "test@example.com";
        const string login = "testuser";
        const string firstName = "Test";
        const string lastName = "User";
        const string nickName = "testy";
        const string password = "Password123!";
        const string image = "image";

        // Act
        var registerDto = new RegisterDto(email, login, firstName, lastName, nickName, password, image);

        // Assert
        Assert.Equal(email, registerDto.Email);
        Assert.Equal(login, registerDto.Login);
        Assert.Equal(firstName, registerDto.FirstName);
        Assert.Equal(lastName, registerDto.LastName);
        Assert.Equal(nickName, registerDto.NickName);
        Assert.Equal(password, registerDto.Password);
    }

    [Fact]
    public void Password_ShouldBeSettable()
    {
        // Arrange
        var registerDto = new RegisterDto(
            "test@example.com",
            "testuser",
            "Test",
            "User",
            "testy",
            "OldPassword123!",
            "test"
            );

        // Act
        const string newPassword = "NewPassword123!";
        registerDto.Password = newPassword;
            
        // Assert
        Assert.Equal(newPassword, registerDto.Password);
    }

    [Fact]
    public void Properties_ShouldHaveCorrectAccessors()
    {
        // Arrange
        var type = typeof(RegisterDto);
            
        // Act & Assert
        var passwordProperty = type.GetProperty("Password");
        Assert.NotNull(passwordProperty);
        Assert.NotNull(passwordProperty.SetMethod);
        Assert.True(passwordProperty.SetMethod.IsPublic);
        
        var emailProperty = type.GetProperty("Email");
        Assert.NotNull(emailProperty);
        Assert.NotNull(emailProperty.SetMethod);

        var loginProperty = type.GetProperty("Login");
        Assert.NotNull(loginProperty);
        Assert.NotNull(loginProperty.SetMethod);
            
        var firstNameProperty = type.GetProperty("FirstName");
        Assert.NotNull(firstNameProperty);
        Assert.NotNull(firstNameProperty.SetMethod);
            
        var lastNameProperty = type.GetProperty("LastName");
        Assert.NotNull(lastNameProperty);
        Assert.NotNull(lastNameProperty.SetMethod);
            
        var nickNameProperty = type.GetProperty("NickName");
        Assert.NotNull(nickNameProperty);
        Assert.NotNull(nickNameProperty.SetMethod);
    }

    [Fact]
    public void Constructor_WithNullParameters_ShouldNotThrowException()
    {
        // Act & Assert
        var exception = Record.Exception(() => new RegisterDto("", "", "", "", "", "", ""));
        Assert.Null(exception);
    }
        
    [Fact]
    public void Object_Initialization_ShouldWork()
    {
        // Arrange & Act
        var registerDto = new RegisterDto(
                "test@example.com", 
                "testuser", 
                "Test", 
                "User", 
                "testy", 
                "Password123!",
                "tets")
        {
            Email = "new@example.com",
            Login = "newlogin",
            FirstName = "NewFirst",
            LastName = "NewLast",
            NickName = "newnick",
            Password = "NewPassword123!" 
        };
            
        // Assert
        Assert.Equal("new@example.com", registerDto.Email);
        Assert.Equal("newlogin", registerDto.Login);
        Assert.Equal("NewFirst", registerDto.FirstName);
        Assert.Equal("NewLast", registerDto.LastName);
        Assert.Equal("newnick", registerDto.NickName);
        Assert.Equal("NewPassword123!", registerDto.Password);
    }
}