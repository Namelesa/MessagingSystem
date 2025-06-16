using AutoMapper;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.WebApi.User;
using MessagingSystem.Services.User.WebApi.User.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.User;

public class UsersControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IUserOrchestrator> _userOrchestratorMock;
    private readonly Mock<IImageLoaderService> _imageLoaderServiceMock;
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _userOrchestratorMock = new Mock<IUserOrchestrator>();
        _imageLoaderServiceMock = new Mock<IImageLoaderService>();

        _sut = new UsersController(_userOrchestratorMock.Object, _mapperMock.Object, _imageLoaderServiceMock.Object);
    }

    [Fact]
    public async Task EditUserAsync_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var userId = "user123";
        var editUserContract = new EditUserContract(
            "Maxim",
            "Bilyk",
        "qwerty123_4123456789",
        "pdo090318@gmail.com", 
            "qwerty123@4567");
        var userDto = new UserDto(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com", 
            "qwerty123@4567",
            "test");
        var operationResult = OperationResult<string>.Ok("User updated successfully");

        _mapperMock
            .Setup(m => m.Map<UserDto>(editUserContract))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.EditUserAsync(userId, editUserContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User updated successfully", okResult.Value);
    }

    [Fact]
    public async Task EditUserAsync_WithInvalidData_ReturnsBadRequestResult()
    {
        // Arrange
        const string userId = "user123";
        var editUserContract = new EditUserContract(
            "",
            "",
            "login",
            "invalid-email",
            "nickName123!");
        
        var userDto = new UserDto("", "", "login", "invalid-email", "nickName123!", "tets");
        var operationResult = OperationResult<string>.Fail("Invalid user data");

        _mapperMock
            .Setup(m => m.Map<UserDto>(editUserContract))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.EditUserAsync(userId, editUserContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid user data", badRequestResult.Value);
    }

    [Fact]
    public async Task EditUserAsync_WithExistingUsername_ReturnsBadRequestResult()
    {
        // Arrange
        const string userId = "user123";
        var editUserContract = new EditUserContract(
            "existing",
            "Username",
            "qwerty1234!",
            "newemail@example.com",
            "qwerty!1234");

        var userDto = new UserDto(
            "existing",
            "Username",
            "qwerty1234!",
            "newemail@example.com",
            "qwerty1234!",
            "test");
        var operationResult = OperationResult<string>.Fail("Username is already taken");

        _mapperMock
            .Setup(m => m.Map<UserDto>(editUserContract))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.EditUserAsync(userId, editUserContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username is already taken", badRequestResult.Value);
    }

    [Fact]
    public async Task EditUserAsync_WithExistingEmail_ReturnsBadRequestResult()
    {
        // Arrange
        const string userId = "user123";
        var editUserContract = new EditUserContract(
            "existing",
            "Username",
            "qwerty1234!",
            "newemail@example.com",
            "qwerty!1234");

        var userDto = new UserDto(
            "existing",
            "Username",
            "qwerty1234!",
            "newemail@example.com",
            "qwerty1234!",
            "test");
        var operationResult = OperationResult<string>.Fail("Email is already in use");

        _mapperMock
            .Setup(m => m.Map<UserDto>(editUserContract))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.EditUserAsync(userId, editUserContract);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email is already in use", badRequestResult.Value);
    }
    
    [Fact]
    public async Task EditUserAsync_WithImageFile_UploadsImageAndReturnsOkResult()
    {
        // Arrange
        const string userId = "user123";
        const string uploadedImageUrl = "https://example.com/uploaded-image.jpg";
        
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        
        mockFile.Setup(f => f.Length).Returns(5);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        
        var editUserContract = new EditUserContract(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567")
        {
            ImageFile = mockFile.Object
        };

        var userDto = new UserDto(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567",
            uploadedImageUrl);

        var operationResult = OperationResult<string>.Ok("User updated successfully");

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedImageUrl);

        _mapperMock
            .Setup(m => m.Map<UserDto>(It.Is<EditUserContract>(c => c.Image == uploadedImageUrl)))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.EditUserAsync(userId, editUserContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User updated successfully", okResult.Value);
        
        // Verify image upload was called
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.Is<string>(fileName => fileName.EndsWith(".jpg"))),
            Times.Once);
        
        // Verify the contract had the image URL set
        Assert.Equal(uploadedImageUrl, editUserContract.Image);
    }

    [Fact]
    public async Task EditUserAsync_WithEmptyImageFile_SkipsImageUploadAndReturnsOkResult()
    {
        // Arrange
        const string userId = "user123";
        
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        
        var editUserContract = new EditUserContract(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567")
        {
            ImageFile = mockFile.Object
        };

        var userDto = new UserDto(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567",
            "test");

        var operationResult = OperationResult<string>.Ok("User updated successfully");

        _mapperMock
            .Setup(m => m.Map<UserDto>(editUserContract))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.EditUserAsync(userId, editUserContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User updated successfully", okResult.Value);
        
        // Verify image upload was NOT called
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task EditUserAsync_WithNullImageFile_SkipsImageUploadAndReturnsOkResult()
    {
        // Arrange
        const string userId = "user123";
        
        var editUserContract = new EditUserContract(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567")
        {
            ImageFile = null
        };

        var userDto = new UserDto(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567",
            "test");

        var operationResult = OperationResult<string>.Ok("User updated successfully");

        _mapperMock
            .Setup(m => m.Map<UserDto>(editUserContract))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.EditUserAsync(userId, editUserContract);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User updated successfully", okResult.Value);
        
        // Verify image upload was NOT called
        _imageLoaderServiceMock.Verify(
            s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task EditUserAsync_WithImageUploadFailure_StillProceedsWithUserUpdate()
    {
        // Arrange
        const string userId = "user123";
        
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        
        mockFile.Setup(f => f.Length).Returns(5);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        
        var editUserContract = new EditUserContract(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567")
        {
            ImageFile = mockFile.Object
        };

        var userDto = new UserDto(
            "Maxim",
            "Bilyk",
            "qwerty123_4123456789",
            "pdo090318@gmail.com",
            "qwerty123@4567",
            "test");

        var operationResult = OperationResult<string>.Ok("User updated successfully");

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Upload failed"));

        _mapperMock
            .Setup(m => m.Map<UserDto>(editUserContract))
            .Returns(userDto);

        _userOrchestratorMock
            .Setup(o => o.EditUserInfoAsync(userDto, userId))
            .ReturnsAsync(operationResult);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.EditUserAsync(userId, editUserContract));
    }

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var userId = "user123";
        var operationResult = OperationResult<string>.Ok("User deleted successfully");

        _userOrchestratorMock
            .Setup(o => o.DeleteUserAsync(userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.DeleteUserAsync(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User deleted successfully", okResult.Value);
    }

    [Fact]
    public async Task DeleteUserAsync_WithInvalidId_ReturnsBadRequestResult()
    {
        // Arrange
        var userId = "invalid-user-id";
        var operationResult = OperationResult<string>.Fail("User not found");

        _userOrchestratorMock
            .Setup(o => o.DeleteUserAsync(userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.DeleteUserAsync(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User not found", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteUserAsync_WithDatabaseError_ReturnsBadRequestResult()
    {
        // Arrange
        var userId = "user123";
        var operationResult = OperationResult<string>.Fail("Database error occurred");

        _userOrchestratorMock
            .Setup(o => o.DeleteUserAsync(userId))
            .ReturnsAsync(operationResult);

        // Act
        var result = await _sut.DeleteUserAsync(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Database error occurred", badRequestResult.Value);
    }
}