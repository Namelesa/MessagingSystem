using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.User.WebApi.Register.Contracts;
using Microsoft.AspNetCore.Http;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Register;

public class RegisterContractInitTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties_WithCorrectValues()
    {
        // Arrange
        const string firstName = "John";
        const string lastName = "Doe";
        const string login = "@user123";
        const string email = "john.doe@example.com";
        const string nickName = "nick_123";
        const string password = "Pass123@";

        // Act
        var contract = new RegisterContract(firstName, lastName, login, email, nickName, password);

        // Assert
        Assert.Equal(firstName, contract.FirstName);
        Assert.Equal(lastName, contract.LastName);
        Assert.Equal(login, contract.Login);
        Assert.Equal(email, contract.Email);
        Assert.Equal(nickName, contract.NickName);
        Assert.Equal(password, contract.Password);
    }

    [Fact]
    public void DefaultConstructor_ShouldInitializeWithEmptyValues()
    {
        // Act
        var contract = new RegisterContract();

        // Assert
        Assert.Equal(string.Empty, contract.FirstName);
        Assert.Equal(string.Empty, contract.LastName);
        Assert.Equal(string.Empty, contract.Login);
        Assert.Equal(string.Empty, contract.Email);
        Assert.Equal(string.Empty, contract.NickName);
        Assert.Equal(string.Empty, contract.Password);
        Assert.Null(contract.Image);
        Assert.Null(contract.AvatarUrl);
    }
    
    [Fact]
    public void ObjectInitializer_ShouldSetInitPropertiesCorrectly()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@user123", "john.doe@example.com", "nick_123", "Pass123@")
        {
            FirstName = "NewFirst",
            LastName = "NewLast",
            Login = "@newLogin",
            NickName = "new_nick123"
        };

        // Assert
        Assert.Equal("NewFirst", contract.FirstName);
        Assert.Equal("NewLast", contract.LastName);
        Assert.Equal("@newLogin", contract.Login);
        Assert.Equal("new_nick123", contract.NickName);
    }

    [Fact]
    public void Validation_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var context = new ValidationContext(contract);
        var isValid = Validator.TryValidateObject(contract, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void Validation_ShouldFail_WhenFirstNameIsInvalid()
    {
        // Arrange
        var contract = new RegisterContract(
            "Jo", "Doe", "@login1", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("First name"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenPasswordIsMissingSpecialCharacter()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "nick@name", "Password123");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Password must be"));
    }
    
    [Fact]
    public void Validation_ShouldFail_WhenNickNameTooShort()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "@", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Nick name must be"));
    }
    [Fact]
    public void Constructor_WithImage_ShouldInitializeImageCorrectly()
    {
        // Arrange
        var mockImage = new Mock<IFormFile>();
        
        // Act
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "nick@name", "Pass123@", mockImage.Object);

        // Assert
        Assert.Equal("John", contract.FirstName);
        Assert.Equal("Doe", contract.LastName);
        Assert.Equal("@login1", contract.Login);
        Assert.Equal("john@example.com", contract.Email);
        Assert.Equal("nick@name", contract.NickName);
        Assert.Equal("Pass123@", contract.Password);
        Assert.Equal(mockImage.Object, contract.Image);
    }

    [Fact]
    public void AvatarUrl_ShouldBeSettable()
    {
        // Arrange
        var contract = new RegisterContract();
        const string avatarUrl = "https://example.com/avatar.jpg";

        // Act
        contract.AvatarUrl = avatarUrl;

        // Assert
        Assert.Equal(avatarUrl, contract.AvatarUrl);
    }

    [Fact]
    public void Validation_ShouldFail_WhenLastNameIsInvalid()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Do", "@login1", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Last name"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenLastNameTooLong()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "ThisIsAVeryLongLastNameThatExceedsTheLimit", "@login1", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Last name"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenLoginTooShort()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@lo", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Login"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenLoginTooLong()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@thisIsAVeryLongLoginThatExceedsTheLimit", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Login"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenLoginMissingSpecialCharacter()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "login123", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Login"));
    }
    
    [Fact]
    public void Validation_ShouldFail_WhenEmailIsEmpty()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null);
    }

    [Fact]
    public void Validation_ShouldFail_WhenNickNameTooLong()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "ThisIsAVeryLongNickName@", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Nick name"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenNickNameMissingSpecialCharacter()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "nickname123", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Nick name"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenPasswordTooShort()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "nick@name", "P1@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Password must be"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenPasswordMissingLetter()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "nick@name", "12345@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Password must be"));
    }

    [Fact]
    public void Validation_ShouldFail_WhenPasswordMissingNumber()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@login1", "john@example.com", "nick@name", "Password@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Password must be"));
    }

    [Fact]
    public void Validation_ShouldPass_WithCyrillicCharacters()
    {
        // Arrange
        var contract = new RegisterContract(
            "Максим", "Билык", "@login1", "john@example.com", "nick@name", "Pass123@");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void Validation_ShouldFail_WhenAllFieldsAreEmpty()
    {
        // Arrange
        var contract = new RegisterContract();

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.True(results.Count >= 5); // At least 5 required fields should fail
    }

    [Theory]
    [InlineData("login_123", "nick_name", "Pass123@")] // Valid with underscore
    [InlineData("login!123", "nick!name", "Pass123!")] // Valid with exclamation
    [InlineData("login@123", "nick@name", "Pass123@")] // Valid with at symbol
    public void Validation_ShouldPass_WithDifferentSpecialCharacters(string login, string nickName, string password)
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", login, "john@example.com", nickName, password);

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }
}
