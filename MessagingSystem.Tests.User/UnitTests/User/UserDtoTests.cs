using MessagingSystem.Services.User.Application.User;

namespace MessagingSystem.Tests.User.UnitTests.User;

public class UserDtoTests
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

        // Act
        var userDto = new UserDto(firstName, lastName, login, email, nickName);

        // Assert
        Assert.Equal(email, userDto.Email);
        Assert.Equal(login, userDto.Login);
        Assert.Equal(firstName, userDto.FirstName);
        Assert.Equal(lastName, userDto.LastName);
        Assert.Equal(nickName, userDto.NickName);
    }
    
    [Fact]
    public void Properties_ShouldHaveCorrectAccessors()
    {
        // Arrange
        var type = typeof(UserDto);
            
        // Act & Assert
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
        var exception = Record.Exception(() => new UserDto("", "", "", "", ""));
        Assert.Null(exception);
    }
        
    [Fact]
    public void Object_Initialization_ShouldWork()
    {
        // Arrange & Act
        var userDto = new UserDto(
            "Test",
            "User", 
            "testy",
            "test@example.com",
            "testNick")
        {
            Email = "new@example.com",
            Login = "newlogin",
            FirstName = "NewFirst",
            LastName = "NewLast",
            NickName = "newnick",
        };
            
        // Assert
        Assert.Equal("new@example.com", userDto.Email);
        Assert.Equal("newlogin", userDto.Login);
        Assert.Equal("NewFirst", userDto.FirstName);
        Assert.Equal("NewLast", userDto.LastName);
        Assert.Equal("newnick", userDto.NickName);
    }
}