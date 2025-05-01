using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.User.WebApi.User.Contracts;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.User;

public class UserContractTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties_Correctly()
    {
        // Arrange
        const string firstName = "John";
        const string lastName = "Doe";
        const string login = "@login123";
        const string email = "john.doe@example.com";
        const string nickName = "nick_@name";

        // Act
        var contract = new EditUserContract(firstName, lastName, login, email, nickName);

        // Assert
        Assert.Equal(firstName, contract.FirstName);
        Assert.Equal(lastName, contract.LastName);
        Assert.Equal(login, contract.Login);
        Assert.Equal(email, contract.Email);
        Assert.Equal(nickName, contract.NickName);
    }

    [Fact]
    public void Validation_ShouldPass_WithValidFields()
    {
        // Arrange
        var contract = new EditUserContract("John", "Smith", "@login1", "john@example.com", "nick@name");

        // Act
        var results = new List<ValidationResult>();
        var context = new ValidationContext(contract);
        var isValid = Validator.TryValidateObject(contract, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Jo")] 
    [InlineData("ThisNameIsWayTooLongToBeValid")] 
    public void Validation_ShouldFail_WhenFirstNameIsInvalid(string invalidFirstName)
    {
        var contract = new EditUserContract(invalidFirstName, "Smith", "@login1", "john@example.com", "nick@name");

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("First name length"));
    }

    [Theory]
    [InlineData("Li")] 
    [InlineData("ThisIsAVeryVeryLongLastNameIndeed")]
    public void Validation_ShouldFail_WhenLastNameIsInvalid(string invalidLastName)
    {
        var contract = new EditUserContract("John", invalidLastName, "@login1", "john@example.com", "nick@name");

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Last name length"));
    }

    [Theory]
    [InlineData("login")] 
    [InlineData("lo")]    
    [InlineData("thisloginiswaytoolongandinvalid!")] 
    public void Validation_ShouldFail_WhenLoginIsInvalid(string invalidLogin)
    {
        var contract = new EditUserContract("John", "Smith", invalidLogin, "john@example.com", "nick@name");

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Login must be"));
    }
    
    [Theory]
    [InlineData("ni")] 
    [InlineData("nicknameistoolong@")] 
    [InlineData("nickname")] 
    public void Validation_ShouldFail_WhenNickNameIsInvalid(string invalidNick)
    {
        var contract = new EditUserContract("John", "Smith", "@login1", "john@example.com", invalidNick);

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Nick name must be"));
    }
}
