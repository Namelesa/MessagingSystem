using Encryptor.Decryption;
using FluentValidation;
using FluentValidation.Results;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;
using Moq;

namespace MessagingSystem.Tests.Notification.UnitTests.Application.Notification;

public class NotificationOrchestratorTests
{
    private readonly Mock<INotification> _notificationMock = new();
    private readonly Mock<IValidator<UserDto>> _validatorMock = new();
    private readonly Mock<IDecryptionInfo> _decryptInfoMock = new();
    private readonly NotificationOrchestrator _orchestrator;

    public NotificationOrchestratorTests()
    {
        _orchestrator = new NotificationOrchestrator(
            _notificationMock.Object,
            _decryptInfoMock.Object,
            _validatorMock.Object);
    }

    private static UserDto CreateValidUser() => new("tester", "test@example.com");

    private void SetupValidationResult(bool isValid, params string[] errors)
    {
        var validationResult = new ValidationResult();
        if (!isValid)
        {
            foreach (var error in errors)
                validationResult.Errors.Add(new ValidationFailure("Field", error));
        }

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
            .ReturnsAsync(validationResult);
    }

    [Fact]
    public async Task SendConfirmEmailAsync_Success()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidationResult(true);
        _notificationMock
            .Setup(n => n.SendConfirmEmailAsync(user, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _orchestrator.SendConfirmEmailAsync(user, "NickName");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User notified", result.Data);
    }

    [Fact]
    public async Task SendConfirmEmailAsync_ValidationFails()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidationResult(false, "Invalid email");

        // Act
        var result = await _orchestrator.SendConfirmEmailAsync(user, "NickName");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid email", result.Message);
    }

    [Fact]
    public async Task SendConfirmEmailAsync_SendFails()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidationResult(true);
        _notificationMock
            .Setup(n => n.SendConfirmEmailAsync(user, It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _orchestrator.SendConfirmEmailAsync(user, "NickName");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User was not notified", result.Message);
    }

    [Fact]
    public async Task SendEditUserInfoEmailAsync_Success()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidationResult(true);
        _notificationMock
            .Setup(n => n.SendEditUserInfoEmailAsync(user))
            .ReturnsAsync(true);

        // Act
        var result = await _orchestrator.SendEditUserInfoEmailAsync(user);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SendDeleteUserInfoEmailAsync_Failure()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidationResult(true);
        _notificationMock
            .Setup(n => n.SendDeleteUserEmailAsync(user))
            .ReturnsAsync(false);

        // Act
        var result = await _orchestrator.SendDeleteUserInfoEmailAsync(user);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User was not notified", result.Message);
    }

    [Fact]
    public async Task ValidateAndEncryptAsync_CallsDecryptionMethods()
    {
        // Arrange
        var user = CreateValidUser();
        SetupValidationResult(true);

        // Act
        await _orchestrator.SendEditUserInfoEmailAsync(user);

        // Assert
        _decryptInfoMock.Verify(d => d.DecryptRsaObjectStrings(user), Times.Once);
        _decryptInfoMock.Verify(d => d.DecryptObjectStrings(user), Times.Once);
    }
}