using FluentAssertions;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;

namespace MessagingSystem.Tests.User.UnitTests.Application.Auth.Register;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    private RegisterDto CreateValidDto(
        string email = "pdo090318@gmail.com",
        string login = "qwerty123_4123456789",
        string firstName = "Maxim",
        string lastName = "Bilyk",
        string nickName = "qwerty123@4567",
        string password = "Test123!4987654")
    {
        return new RegisterDto(email, login, firstName, lastName, nickName, password);
    }
    
    [Fact]
    public void RegisterUser_WhenDataIsValid_ShouldReturnSuccess()
    {
        var dto = CreateValidDto();
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("pdo090318")] 
    [InlineData("")] 
    public void RegisterUser_WhenEmailIsInvalid_ShouldReturnFail(string email)
    {
        var dto = CreateValidDto(email: email);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("qwerty123")]
    [InlineData("")]
    public void RegisterUser_WhenLoginIsInvalid_ShouldReturnFail(string login)
    {
        var dto = CreateValidDto(login: login);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("M")] 
    [InlineData("")]
    public void RegisterUser_WhenFirstNameIsInvalid_ShouldReturnFail(string firstName)
    {
        var dto = CreateValidDto(firstName: firstName);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("B")]
    [InlineData("")] 
    public void RegisterUser_WhenLastNameIsInvalid_ShouldReturnFail(string lastName)
    {
        var dto = CreateValidDto(lastName: lastName);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("q")]
    [InlineData("")] 
    public void RegisterUser_WhenNickNameIsInvalid_ShouldReturnFail(string nickName)
    {
        var dto = CreateValidDto(nickName: nickName);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("t")] 
    [InlineData("")] 
    public void RegisterUser_WhenPasswordIsInvalid_ShouldReturnFail(string password)
    {
        var dto = CreateValidDto(password: password);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterUser_WhenAllDataIsInvalid_ShouldReturnFail()
    {
        var dto = CreateValidDto("", "", "", "", "", "");
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }
}
