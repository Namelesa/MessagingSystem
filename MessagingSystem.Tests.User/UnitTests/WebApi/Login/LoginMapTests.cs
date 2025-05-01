using AutoMapper;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.WebApi.Login;
using MessagingSystem.Services.User.WebApi.Login.Contracts;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Login;

public class LoginMapTests
{
    private readonly IMapper _mapper;

    public LoginMapTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<LoginMap>();
        });

        config.AssertConfigurationIsValid();

        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Should_Map_LoginContract_To_LoginDto_Correctly()
    {
        // Arrange
        var contract = new LoginContract("testUser@", "Pass123@");

        // Act
        var dto = _mapper.Map<LoginDto>(contract);

        // Assert
        Assert.Equal(contract.Login, dto.Login);
        Assert.Equal(contract.Password, dto.Password);
    }
}