using FluentAssertions;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;

namespace MessagingSystem.Tests.User.UnitTests.Application.User;

public class UserValidatorTests
{
    private readonly UserValidator _validator = new();
    
    private static UserDto CreateValidDto(
        string email = "pdo090318@gmail.com",
        string login = "qwerty123_4123456789",
        string firstName = "Maxim",
        string lastName = "Bilyk",
        string nickName = "qwerty123@4567",
        string image = "test")
    {
        return new UserDto(firstName, lastName, login, email, nickName, image);
    }

    [Fact]
    public void CreateUser_WhenDataIsValid_ShouldReturnSuccess()
    {
        var dto = CreateValidDto();
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("pdo090318")] 
    [InlineData("")] 
    public void CreateUser_WhenEmailIsInvalid_ShouldReturnFail(string email)
    {
        var dto = CreateValidDto(email: email);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("qwerty123")]
    [InlineData("")]
    public void CreateUser_WhenLoginIsInvalid_ShouldReturnFail(string login)
    {
        var dto = CreateValidDto(login: login);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("M")] 
    [InlineData("")]
    public void CreateUser_WhenFirstNameIsInvalid_ShouldReturnFail(string firstName)
    {
        var dto = CreateValidDto(firstName: firstName);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("B")]
    [InlineData("")] 
    public void CreateUser_WhenLastNameIsInvalid_ShouldReturnFail(string lastName)
    {
        var dto = CreateValidDto(lastName: lastName);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("q")]
    [InlineData("")] 
    public void CreateUser_WhenNickNameIsInvalid_ShouldReturnFail(string nickName)
    {
        var dto = CreateValidDto(nickName: nickName);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void RegisterUser_WhenAllDataIsInvalid_ShouldReturnFail()
    {
        var dto = CreateValidDto("", "", "", "", "");
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }
    
}