using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Tests.Notification.UnitTests.Core;

public class UserDtoTests
{
    [Theory]
    [InlineData("john_doe", "john@example.com")]
    [InlineData("alice123", "alice@mail.com")]
    [InlineData("", "emptyname@test.com")]
    [InlineData("nullEmail", "")]
    public void Constructor_ShouldInitializeProperties(string userName, string email)
    {
        // Act
        var dto = new UserDto(userName, email);

        // Assert
        Assert.Equal(userName, dto.UserName);
        Assert.Equal(email, dto.Email);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var dto = new UserDto("initial", "initial@example.com")
        {
            // Act
            UserName = "updatedUser",
            Email = "updated@example.com"
        };

        // Assert
        Assert.Equal("updatedUser", dto.UserName);
        Assert.Equal("updated@example.com", dto.Email);
    }
}