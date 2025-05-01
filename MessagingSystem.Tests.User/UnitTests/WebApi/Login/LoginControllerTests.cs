using AutoMapper;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.WebApi.Login;
using MessagingSystem.Services.User.WebApi.Login.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Login;

public class LoginControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILoginOrchestrator> _loginOrchestratorMock;
    private readonly LoginController _sut; 

    public LoginControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _loginOrchestratorMock = new Mock<ILoginOrchestrator>(); 

        _sut = new LoginController(_mapperMock.Object, _loginOrchestratorMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var loginContract = new LoginContract("testuser", "password");
        var loginDto = new LoginDto("testuser", "password");
        var operationResult = OperationResult<string>.Ok("True");

        _mapperMock
            .Setup(m => m.Map<LoginDto>(loginContract))
            .Returns(loginDto);

        _loginOrchestratorMock
            .Setup(o => o.LoginUserAsync(loginDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.LoginAsync(loginContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("True", okResult.Value);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsBadRequestResult()
    {
        // Arrange
        var loginContract = new LoginContract("", "");
        var loginDto = new LoginDto("", "");
        var operationResult = OperationResult<string>.Fail("Invalid credentials");

        _mapperMock
            .Setup(m => m.Map<LoginDto>(loginContract))
            .Returns(loginDto);

        _loginOrchestratorMock
            .Setup(o => o.LoginUserAsync(loginDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.LoginAsync(loginContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid credentials", badRequestResult.Value);
    }

    [Fact]
    public async Task LoginAsync_WithUserNotFound_ReturnsBadRequestResult()
    {
        // Arrange
        var loginContract = new LoginContract("nonexistent", "password");
        var loginDto = new LoginDto("nonexistent", "password");
        var operationResult = OperationResult<string>.Fail("User not found");

        _mapperMock
            .Setup(m => m.Map<LoginDto>(loginContract))
            .Returns(loginDto);

        _loginOrchestratorMock
            .Setup(o => o.LoginUserAsync(loginDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.LoginAsync(loginContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User not found", badRequestResult.Value);
    }

    [Fact]
    public async Task LoginAsync_WithEmailNotConfirmed_ReturnsBadRequestResult()
    {
        // Arrange
        var loginContract = new LoginContract("testuser", "password");
        var loginDto = new LoginDto("testuser", "password");
        var operationResult = OperationResult<string>.Fail("Please confirm email");

        _mapperMock
            .Setup(m => m.Map<LoginDto>(loginContract))
            .Returns(loginDto);

        _loginOrchestratorMock
            .Setup(o => o.LoginUserAsync(loginDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.LoginAsync(loginContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please confirm email", badRequestResult.Value);
    }

    [Fact]
    public async Task LoginAsync_WithFailedAuthentication_ReturnsOkResultWithFalse()
    {
        // Arrange
        var loginContract = new LoginContract("testuser", "wrongpassword");
        var loginDto = new LoginDto("testuser", "wrongpassword");
        var operationResult = OperationResult<string>.Ok("False");

        _mapperMock
            .Setup(m => m.Map<LoginDto>(loginContract))
            .Returns(loginDto);

        _loginOrchestratorMock
            .Setup(o => o.LoginUserAsync(loginDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.LoginAsync(loginContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("False", okResult.Value);
    }
}