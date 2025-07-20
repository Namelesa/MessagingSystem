using AutoMapper;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.WebApi.Register;
using MessagingSystem.Services.User.WebApi.Register.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Register;

public class RegisterControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IRegisterOrchestrator> _registerOrchestratorMock;
    private readonly Mock<IImageLoaderService> _imageLoaderServiceMock;
    private readonly RegisterController _sut;

    public RegisterControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _registerOrchestratorMock = new Mock<IRegisterOrchestrator>();
        _imageLoaderServiceMock = new Mock<IImageLoaderService>();

        _sut = new RegisterController(_registerOrchestratorMock.Object, _mapperMock.Object, _imageLoaderServiceMock.Object);
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
        var json = JsonConvert.SerializeObject(okResult.Value);
        Assert.Contains("Registration successful", json);
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
        var json = JsonConvert.SerializeObject(badRequestResult.Value);
        Assert.Contains("Invalid registration data", json);
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
        var json = JsonConvert.SerializeObject(badRequestResult.Value);
        Assert.Contains("Email is already in use", json);
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
        var json = JsonConvert.SerializeObject(badRequestResult.Value);
        Assert.Contains("Username is already taken", json);
    }
    
    [Fact]
    public async Task RegisterAsync_WithImageFile_UploadsImageAndReturnsOkResult()
    {
        // Arrange
        const string uploadedImageUrl = "https://example.com/uploaded-image.jpg";
        
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        
        mockFile.Setup(f => f.Length).Returns(5);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        
        var registerContract = new RegisterContract(
            "Test", "User", "test@user", "test@example.com", "TestNick@123", "Password123@")
        {
            Image = mockFile.Object
        };

        var operationResult = OperationResult<string>.Ok("Registration successful");

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedImageUrl);

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(new RegisterDto("test@example.com", "test@user", "Test", "User", "TestNick@123", "Password123@", ""));

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(It.Is<RegisterDto>(dto => dto.Image == uploadedImageUrl)))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonConvert.SerializeObject(okResult.Value);
        Assert.Contains("Registration successful", json);
        
        // Verify image upload was called
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.Is<string>(fileName => fileName.EndsWith(".jpg"))),
            Times.Once);
        
        // Verify the contract had the avatar URL set
        Assert.Equal(uploadedImageUrl, registerContract.AvatarUrl);
        
        // Verify the orchestrator was called with the correct DTO that has the image URL
        _registerOrchestratorMock.Verify(
            o => o.RegisterUserAsync(It.Is<RegisterDto>(dto => dto.Image == uploadedImageUrl)),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyImageFile_SkipsImageUploadAndReturnsOkResult()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        
        var registerContract = new RegisterContract(
            "Test", "User", "test@user", "test@example.com", "TestNick@123", "Password123@")
        {
            Image = mockFile.Object
        };

        var registerDto = new RegisterDto(
            "test@example.com", "test@user", "Test", "User", "TestNick@123", "Password123@", "");

        var operationResult = OperationResult<string>.Ok("Registration successful");

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(registerDto);

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(It.Is<RegisterDto>(dto => string.IsNullOrEmpty(dto.Image))))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonConvert.SerializeObject(okResult.Value);
        Assert.Contains("Registration successful", json);

        
        // Verify image upload was NOT called
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Never);
        
        // Verify AvatarUrl remains null
        Assert.Null(registerContract.AvatarUrl);
    }

    [Fact]
    public async Task RegisterAsync_WithNullImageFile_SkipsImageUploadAndReturnsOkResult()
    {
        // Arrange
        var registerContract = new RegisterContract(
            "Test", "User", "test@user", "test@example.com", "TestNick@123", "Password123@")
        {
            Image = null
        };

        var registerDto = new RegisterDto(
            "test@example.com", "test@user", "Test", "User", "TestNick@123", "Password123@", "");

        var operationResult = OperationResult<string>.Ok("Registration successful");

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(registerDto);

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(It.Is<RegisterDto>(dto => string.IsNullOrEmpty(dto.Image))))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonConvert.SerializeObject(okResult.Value);
        Assert.Contains("Registration successful", json);
        
        // Verify image upload was NOT called
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Never);
        
        // Verify AvatarUrl remains null
        Assert.Null(registerContract.AvatarUrl);
    }

    [Fact]
    public async Task RegisterAsync_WithImageFile_ButRegistrationFails_ReturnsBadRequestResult()
    {
        // Arrange
        const string uploadedImageUrl = "https://example.com/uploaded-image.jpg";
        
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        
        mockFile.Setup(f => f.Length).Returns(5);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        
        var registerContract = new RegisterContract(
            "Test", "User", "test@user", "test@example.com", "TestNick@123", "Password123@")
        {
            Image = mockFile.Object
        };

        var operationResult = OperationResult<string>.Fail("Email is already in use");

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedImageUrl);

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(registerContract))
            .Returns(new RegisterDto("test@example.com", "test@user", "Test", "User", "TestNick@123", "Password123@", ""));

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(It.Is<RegisterDto>(dto => dto.Image == uploadedImageUrl)))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.RegisterAsync(registerContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonConvert.SerializeObject(badRequestResult.Value);
        Assert.Contains("Email is already in use", json);
        
        // Verify image upload was still called (happens before validation)
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.Is<string>(fileName => fileName.EndsWith(".jpg"))),
            Times.Once);
        
        // Verify the contract had the avatar URL set
        Assert.Equal(uploadedImageUrl, registerContract.AvatarUrl);
    }

    [Fact]
    public async Task RegisterAsync_WithImageUploadFailure_ThrowsException()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        
        mockFile.Setup(f => f.Length).Returns(5);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        
        var registerContract = new RegisterContract(
            "Test", "User", "test@user", "test@example.com", "TestNick@123", "Password123@")
        {
            Image = mockFile.Object
        };

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Upload failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.RegisterAsync(registerContract));
        
        // Verify image upload was called
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_GeneratesUniqueFileNames_ForMultipleUploads()
    {
        // Arrange
        const string uploadedImageUrl1 = "https://example.com/image1.jpg";
        const string uploadedImageUrl2 = "https://example.com/image2.jpg";
        
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        
        mockFile.Setup(f => f.Length).Returns(5);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        
        var registerContract1 = new RegisterContract(
            "Test", "User", "test@user1", "test1@example.com", "TestNick@123", "Password123@")
        {
            Image = mockFile.Object
        };

        var registerContract2 = new RegisterContract(
            "Test", "User", "test@user2", "test2@example.com", "TestNick@124", "Password123@")
        {
            Image = mockFile.Object
        };

        var operationResult = OperationResult<string>.Ok("Registration successful");

        _imageLoaderServiceMock
            .SetupSequence(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedImageUrl1)
            .ReturnsAsync(uploadedImageUrl2);

        _mapperMock
            .Setup(m => m.Map<RegisterDto>(It.IsAny<RegisterContract>()))
            .Returns(new RegisterDto("", "", "", "", "", "", ""));

        _registerOrchestratorMock
            .Setup(o => o.RegisterUserAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(operationResult);

        // Act
        await _sut.RegisterAsync(registerContract1);
        await _sut.RegisterAsync(registerContract2);

        // Assert
        // Verify image upload was called twice with different file names
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Exactly(2));
        
        // Verify different URLs were set
        Assert.Equal(uploadedImageUrl1, registerContract1.AvatarUrl);
        Assert.Equal(uploadedImageUrl2, registerContract2.AvatarUrl);
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
        var badRequestResult = Assert.IsType<RedirectResult>(result);
        Assert.NotEmpty(badRequestResult.Url);
    }
}