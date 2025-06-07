using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.User.WebApi.Register.Contracts;

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
        const string image = "testImage";

        // Act
        var contract = new RegisterContract(firstName, lastName, login, email, nickName, password, image);

        // Assert
        Assert.Equal(firstName, contract.FirstName);
        Assert.Equal(lastName, contract.LastName);
        Assert.Equal(login, contract.Login);
        Assert.Equal(email, contract.Email);
        Assert.Equal(nickName, contract.NickName);
        Assert.Equal(password, contract.Password);
    }

    [Fact]
    public void ObjectInitializer_ShouldSetInitPropertiesCorrectly()
    {
        // Arrange
        var contract = new RegisterContract(
            "John", "Doe", "@user123", "john.doe@example.com", "nick_123", "Pass123@", "")
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
            "John", "Doe", "@login1", "john@example.com", "nick@name", "Pass123@", "");

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
            "Jo", "Doe", "@login1", "john@example.com", "nick@name", "Pass123@", "");

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
            "John", "Doe", "@login1", "john@example.com", "nick@name", "Password123", "");

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
            "John", "Doe", "@login1", "john@example.com", "@", "Pass123@", "");

        // Act
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Nick name must be"));
    }
}
