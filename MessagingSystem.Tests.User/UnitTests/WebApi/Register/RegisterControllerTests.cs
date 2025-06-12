using AutoMapper;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.WebApi.Register;
using MessagingSystem.Services.User.WebApi.Register.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Register;

public class RegisterControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IRegisterOrchestrator> _registerOrchestratorMock;
    private readonly RegisterController _sut;

    public RegisterControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _registerOrchestratorMock = new Mock<IRegisterOrchestrator>();
        Mock<IImageLoaderService> imageLoaderServiceMock = new();

        _sut = new RegisterController(_registerOrchestratorMock.Object, _mapperMock.Object, imageLoaderServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var registerContract = new RegisterContract("Testt", "Users","testuser", "test@example.com", "TestNick123!", "Password123!");
        var registerDto = new RegisterDto("test@example.com", "testuser", "Testt", "Users", "TestNick123!", "Password123!", "");
        var operationResult = OperationResult<string>.Ok("Registration successful");

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(registerDto);

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(registerDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Registration successful", okResult.Value);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidData_ReturnsBadRequestResult()
    {
        // Arrange
        var registerContract = new RegisterContract("", "", "", "", "", "");
        var registerDto = new RegisterDto("", "", "", "", "", "", "");
        var operationResult = OperationResult<string>.Fail("Invalid registration data");

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(registerDto);

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(registerDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid registration data", badRequestResult.Value);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsBadRequestResult()
    {
        // Arrange
        var registerContract = new RegisterContract("Testt", "Users","testuser", "existing@example.com", "TestNick123!", "Password123!");
        var registerDto = new RegisterDto("existing@example.com", "testuser", "Testt", "Users", "TestNick123!", "Password123!", "");
        var operationResult = OperationResult<string>.Fail("Email is already in use");

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(registerDto);

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(registerDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email is already in use", badRequestResult.Value);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ReturnsBadRequestResult()
    {
        // Arrange
        var registerContract = new RegisterContract("Testt", "Users","testuser", "test@example.com", "TestNick123!", "Password123!");
        var registerDto = new RegisterDto("test@example.com", "testuser", "Testt", "Users", "TestNick123!", "Password123!", "");
        var operationResult = OperationResult<string>.Fail("Username is already taken");

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(registerDto);

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(registerDto))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username is already taken", badRequestResult.Value);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidId_ReturnsOkResult()
    {
        // Arrange
        const string id = "valid-confirmation-id";
        var operationResult = OperationResult<string>.Ok("Email confirmed successfully");

        _registerOrchestratorMock
            .Setup(o => o.ConfirmEmailAsync(id))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.ConfirmEmailAsync(id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Email confirmed successfully", okResult.Value);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithInvalidId_ReturnsBadRequestResult()
    {
        // Arrange
        const string id = "invalid-confirmation-id";
        var operationResult = OperationResult<string>.Fail("Invalid confirmation ID");

        _registerOrchestratorMock
            .Setup(o => o.ConfirmEmailAsync(id))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.ConfirmEmailAsync(id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid confirmation ID", badRequestResult.Value);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithExpiredId_ReturnsBadRequestResult()
    {
        // Arrange
        const string id = "expired-confirmation-id";
        var operationResult = OperationResult<string>.Fail("Confirmation link has expired");

        _registerOrchestratorMock
            .Setup(o => o.ConfirmEmailAsync(id))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.ConfirmEmailAsync(id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Confirmation link has expired", badRequestResult.Value);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithAlreadyConfirmedEmail_ReturnsBadRequestResult()
    {
        // Arrange
        const string id = "already-confirmed-id";
        var operationResult = OperationResult<string>.Fail("Email is already confirmed");

        _registerOrchestratorMock
            .Setup(o => o.ConfirmEmailAsync(id))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.ConfirmEmailAsync(id);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email is already confirmed", badRequestResult.Value);
    }
}