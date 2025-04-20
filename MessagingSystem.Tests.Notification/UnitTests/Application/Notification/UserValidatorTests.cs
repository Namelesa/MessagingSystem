using FluentAssertions;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;
using Xunit.Abstractions;

namespace MessagingSystem.Tests.Notification.UnitTests.Application.Notification;

public class UserValidatorTests(ITestOutputHelper output)
{
    private readonly UserValidator _validator = new();

    [Fact]
    public void Validate_ValidUserDto_ShouldPassValidation()
    {
        // Arrange
        var userDto = new UserDto("userTest", "test@gmail.com");

        // Act
        var result = _validator.Validate(userDto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "User name cannot be empty.")]
    [InlineData("usr", "User name must not be less than 6 characters.")]
    [InlineData("userNameThatIsTooLongForValidationValidationuserNameThatIsTooLongForValidationValidation", "User name must be at most 50 characters long.")]
    [InlineData("userWithoutSpecialChar2123", "User name can only contain letters.")]
    public void Validate_InvalidUserName_ShouldFailValidation(string userName, string testName)
    {
        // Arrange
        output.WriteLine($"Testing scenario: {testName}");
        var userDto = new UserDto(userName, "Valid1@gmail.com");

        // Act
        var result = _validator.Validate(userDto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UserName");
    }

    [Theory]
    [InlineData("", "Email cannot be empty.")]
    [InlineData("pass", "Invalid email format.")]
    [InlineData("password123", "Invalid email format.")]
    [InlineData("pass@", "Invalid email format.")]
    public void Validate_InvalidEmail_ShouldFailValidation(string email, string testName)
    {
        // Arrange
        output.WriteLine($"Testing scenario: {testName}");
        var userDto = new UserDto("valid@user", email);

        // Act
        var result = _validator.Validate(userDto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Email");
    }

    [Fact]
    public void Validate_InvalidUserNameAndEmail_ShouldFailValidationWithMultipleErrors()
    {
        // Arrange
        var userDto = new UserDto("user", "pass");
        
        // Act
        var result = _validator.Validate(userDto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UserName");
        result.Errors.Should().Contain(error => error.PropertyName == "Email");
        result.Errors.Count.Should().Be(2);
    }

    [Theory]
    [InlineData("userTest", "Pass1@gmail.com", true, "Valid UserName and Email")]
    [InlineData("user", "Pass1@gmail.com", false, "Invalid userName")]
    [InlineData("user@1", "password", false, "Invalid email format")]
    [InlineData("", "", false, "Empty userName and Email")]
    public void Validate_VariousInputs_ShouldValidateCorrectly(string userName, string email, bool expectedIsValid, string testName)
    {
        // Arrange
        output.WriteLine($"Testing scenario: {testName}");
        var userDto = new UserDto(userName, email);

        // Act
        var result = _validator.Validate(userDto);

        // Assert
        result.IsValid.Should().Be(expectedIsValid);
    }

    [Fact]
    public void Validate_UserNameWithSpecialCharacters_ShouldFailValidation()
    {
        // Arrange
        var userDto = new UserDto("userTest!", "Pass1@gmail.com");

        // Act
        var result = _validator.Validate(userDto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_UserNameWithAlphanumericAndSpecialChar_ShouldFailValidation()
    {
        // Arrange
        var userDto = new UserDto("user123@", "Pass1@");

        // Act
        var result = _validator.Validate(userDto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmailWithExactLengthLimits_ShouldValidateCorrectly()
    {
        // Arrange
        var userDto1 = new UserDto("validUser", "Pass1@gmail.com");
        var userDto2 = new UserDto("validUser", "Password12345@gmail.com");

        // Act
        var result1 = _validator.Validate(userDto1);
        var result2 = _validator.Validate(userDto2);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UserNameWithExactLengthLimits_ShouldValidateCorrectly()
    {
        // Arrange
        var userDto1 = new UserDto("userTest", "Pass1@gmail.com");
        var userDto2 = new UserDto("userTests", "Pass2@gmail.com");

        // Act
        var result1 = _validator.Validate(userDto1);
        var result2 = _validator.Validate(userDto2);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }
}