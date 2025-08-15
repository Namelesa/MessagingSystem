using FluentAssertions;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using Xunit.Abstractions;

namespace MessagingSystem.Tests.User.UnitTests.Application.Auth.Login;

public class LoginValidatorTests(ITestOutputHelper output)
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_ValidLoginDto_ShouldPassValidation()
    {
        // Arrange
        var loginDto = new LoginDto("user@1", "Pass1@", "Pass1@");

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Password must be empty")]
    [InlineData("usr", "Login is too short")]
    [InlineData("userNameThatIsTooLongForValidation", "Login is too long")]
    [InlineData("userWithoutSpecialChar", "Login without special characters")]
    public void Validate_InvalidLogin_ShouldFailValidation(string login, string testName)
    {
        // Arrange
        output.WriteLine($"Testing scenario: {testName}");
        var loginDto = new LoginDto(login, "Valid1@", "Valid@1");

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Login");
    }

    [Theory]
    [InlineData("", "Password is empty")]
    [InlineData("pass", "Password without special characters and numbers")]
    [InlineData("password123", "Password without special characters")]
    [InlineData("pass@", "Password without numbers")]
    [InlineData("123!@", "Password without letters")]
    public void Validate_InvalidPassword_ShouldFailValidation(string password, string testName)
    {
        // Arrange
        output.WriteLine($"Testing scenario: {testName}");
        var loginDto = new LoginDto("valid@user", password, "validUser@123");

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Password");
    }

    [Fact]
    public void Validate_InvalidLoginAndPassword_ShouldFailValidationWithMultipleErrors()
    {
        // Arrange
        var loginDto = new LoginDto("usr", "pass", "pass");
        
        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Login");
        result.Errors.Should().Contain(error => error.PropertyName == "Password");
        result.Errors.Count.Should().Be(3);
    }

    [Theory]
    [InlineData("user@1", "Pass1@", true, "Valid login and password")]
    [InlineData("user_name", "P@ssw0rd", true, "Valid login with underscore")]
    [InlineData("user!name", "Abc12@", true, "Valid login with exclamation mark")]
    [InlineData("user", "Pass1@", false, "Invalid short login")]
    [InlineData("user@1", "password", false, "Invalid password without special chars")]
    [InlineData("", "", false, "Empty login and password")]
    public void Validate_VariousInputs_ShouldValidateCorrectly(string login, string password, bool expectedIsValid, string testName)
    {
        // Arrange
        output.WriteLine($"Testing scenario: {testName}");
        var loginDto = new LoginDto(login, password, "sjubviubweoub");

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        result.IsValid.Should().Be(expectedIsValid);
    }

    [Fact]
    public void Validate_LoginWithSpecialCharacters_ShouldPassValidation()
    {
        // Arrange
        var loginDto = new LoginDto("user@_!", "Pass1@", "Pass1@");

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_LoginWithAlphanumericAndSpecialChar_ShouldPassValidation()
    {
        // Arrange
        var loginDto = new LoginDto("user123@", "Pass1@", "Pass1@");

        // Act
        var result = _validator.Validate(loginDto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PasswordWithExactLengthLimits_ShouldValidateCorrectly()
    {
        // Arrange
        var loginDto1 = new LoginDto("valid@user", "P@ss1", "Pass1@");
        var loginDto2 = new LoginDto("valid@user", "P@ssword12345!@", "Pass1@");

        // Act
        var result1 = _validator.Validate(loginDto1);
        var result2 = _validator.Validate(loginDto2);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_LoginWithExactLengthLimits_ShouldValidateCorrectly()
    {
        // Arrange
        var loginDto1 = new LoginDto("usr@1", "P@ss1", "Pass1@");
        var loginDto2 = new LoginDto("user123456789012345@", "P@ss1", "Pass1@");

        // Act
        var result1 = _validator.Validate(loginDto1);
        var result2 = _validator.Validate(loginDto2);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }
}