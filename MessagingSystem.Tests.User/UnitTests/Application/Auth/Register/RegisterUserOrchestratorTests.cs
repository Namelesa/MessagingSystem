using AutoMapper;
using Encryptor.Encryption;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Add;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;
using Moq;
using ValidationResult = FluentValidation.Results.ValidationResult;
using UserModel = MessagingSystem.Services.User.Core.User.User;

namespace MessagingSystem.Tests.User.UnitTests.Application.Auth.Register;

public class RegisterUserOrchestratorTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IValidator<RegisterDto>> _validator = new();
    private readonly Mock<IHasherPassword> _hasherPassword = new();
    private readonly Mock<IEncryptionInfo> _encryptInfo = new();
    private readonly Mock<IHasher> _hasher = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IPublicKeyStorage> _publicKeyStorage = new();
    private readonly Mock<IRequestClient<AddUserRequest>> _client = new();

    private readonly RegisterDto _dto = new(
        "user@example.com", "login123456", "Max", "Bilyk", "nick123456", "P@ssword123", "");

    private readonly RegisterOrchestrator _orchestrator;

    public RegisterUserOrchestratorTests()
    {
        _orchestrator = new RegisterOrchestrator(
            _userRepository.Object,
            _mapper.Object,
            _validator.Object,
            _hasherPassword.Object,
            _encryptInfo.Object,
            _hasher.Object,
            _publishEndpoint.Object,
            _publicKeyStorage.Object,
            _client.Object);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFail_WhenPublicKeyIsMissing()
    {
        _publicKeyStorage.Setup(p => p.Get("Notification")).Returns((string?)null);
        _publicKeyStorage.Setup(p => p.Get("Messaging")).Returns((string?)null);

        var result = await _orchestrator.RegisterUserAsync(_dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Public key for Notification or Messaging service not found");
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFail_WhenValidationFails()
    {
        _publicKeyStorage.Setup(p => p.Get("Notification")).Returns("fake-key");
        _publicKeyStorage.Setup(p => p.Get("Messaging")).Returns("fake-key");

        _validator.Setup(v => v.ValidateAsync(_dto, default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Email", "Invalid") }));

        var result = await _orchestrator.RegisterUserAsync(_dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFail_WhenUserHasNullFields()
    {
        _publicKeyStorage.Setup(p => p.Get("Notification")).Returns("key");
        _publicKeyStorage.Setup(p => p.Get("Messaging")).Returns("key");
        _validator.Setup(v => v.ValidateAsync(_dto, default)).ReturnsAsync(new ValidationResult());
        _hasherPassword.Setup(h => h.Hash(_dto.Password)).Returns("hashed_pwd");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var user = new UserModel("", "","");
        _mapper.Setup(m => m.Map<UserModel>(_dto)).Returns(user);

        var result = await _orchestrator.RegisterUserAsync(_dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User can not have null properties");
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFail_WhenRepositoryThrows()
    {
        _publicKeyStorage.Setup(p => p.Get("Notification")).Returns("key");
        _publicKeyStorage.Setup(p => p.Get("Messaging")).Returns("key");
        _validator.Setup(v => v.ValidateAsync(_dto, default)).ReturnsAsync(new ValidationResult());
        _hasherPassword.Setup(h => h.Hash(_dto.Password)).Returns("hashed_pwd");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");

        var user = new UserModel("login123456", "TopNick123", "testImage")
        {
            Email = "test@gmail.com",
            UserName = "TestValidUser",
        };
        _mapper.Setup(m => m.Map<UserModel>(_dto)).Returns(user);
        _userRepository.Setup(r => r.AddUserAsync(user)).ThrowsAsync(new Exception("DB Error"));

        var result = await _orchestrator.RegisterUserAsync(_dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User can not be added");
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldFailed_WhenDataIsInValid()
    {
        _publicKeyStorage.Setup(p => p.Get("Notification")).Returns("public-key");
        _publicKeyStorage.Setup(p => p.Get("Messaging")).Returns("public-key");
        _validator.Setup(v => v.ValidateAsync(_dto, default)).ReturnsAsync(new ValidationResult());
        _hasherPassword.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_pwd");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");

        var user = new UserModel("login123456", "CoolNickName", "testImage");
        _mapper.Setup(m => m.Map<UserModel>(_dto)).Returns(user);
        _userRepository.Setup(r => r.AddUserAsync(user)).Returns(Task.CompletedTask);
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<ConfirmUserEmail>(), default)).Returns(Task.CompletedTask);

        var result = await _orchestrator.RegisterUserAsync(_dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User can not have null properties");
    }
    
    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFail_WhenUserAlreadyExists()
    {
        // Arrange
        _publicKeyStorage.Setup(p => p.Get("Notification")).Returns("public-key");
        _publicKeyStorage.Setup(p => p.Get("Messaging")).Returns("public-key");
        _validator.Setup(v => v.ValidateAsync(_dto, default)).ReturnsAsync(new ValidationResult());
        _hasherPassword.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_pwd");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        
        _encryptInfo.Setup(e => e.EncryptObjectStrings(It.IsAny<UserModel>()));
        _encryptInfo.Setup(e => e.EncryptRsa(It.IsAny<string>(), It.IsAny<string>())).Returns("encrypted");
        
        var user = new UserModel("login123456", "CoolNickName", "testImage")
        {
            Email = "test@gmail.com",
            UserName = "TestValidUser",
            NormalizedEmail = "TEST@GMAIL.COM",
            NormalizedUserName = "TESTVALIDUSER"
        };
        _mapper.Setup(m => m.Map<UserModel>(_dto)).Returns(user);

        var addUserResponse = new AddUserResponse(false);
        var mockResponse = Mock.Of<Response<AddUserResponse>>(r => r.Message == addUserResponse);
        _client.Setup(c => c.GetResponse<AddUserResponse>(It.IsAny<AddUserRequest>(), default, default))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _orchestrator.RegisterUserAsync(_dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User already exists");
    }
    
    [Fact]
    public async Task RegisterUserAsync_ShouldSuccess_WhenDataIsValid()
    {
        _publicKeyStorage.Setup(p => p.Get("Notification")).Returns("public-key");
        _publicKeyStorage.Setup(p => p.Get("Messaging")).Returns("public-key");
        _validator.Setup(v => v.ValidateAsync(_dto, default)).ReturnsAsync(new ValidationResult());
        _hasherPassword.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_pwd");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");

        var user = new UserModel("login123456", "CoolNickName", "testImage")
        {
            Email = "test@gmail.com",
            UserName = "TestValidUser",
            NormalizedEmail = "TEST@GMAIL.COM",
            NormalizedUserName = "TESTVALIDUSER"
        };

        _mapper.Setup(m => m.Map<UserModel>(_dto)).Returns(user);
        
        var mockResponse = new Mock<Response<AddUserResponse>>();
        mockResponse.Setup(r => r.Message).Returns(new AddUserResponse(true));
        _client.Setup(c => c.GetResponse<AddUserResponse>(It.IsAny<AddUserRequest>(), default, default))
            .ReturnsAsync(mockResponse.Object);

        _userRepository.Setup(r => r.AddUserAsync(user)).Returns(Task.CompletedTask);
        _publishEndpoint.Setup(p => p.Publish(It.IsAny<ConfirmUserEmail>(), default)).Returns(Task.CompletedTask);

        var result = await _orchestrator.RegisterUserAsync(_dto);

        result.Success.Should().BeTrue();
        result.Data.Should().Contain("User registered and need to confirm email");
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<ConfirmUserEmail>(), default), Times.Once);
    }
    
    [Fact]
    public async Task ConfirmEmailAsync_ShouldFail_WhenUserNotFound()
    {
        _userRepository.Setup(r => r.FindUserByHashNickNameAsync("hash")).ReturnsAsync((UserModel?)null);

        var result = await _orchestrator.ConfirmEmailAsync("hash");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldFail_WhenRepositoryThrows()
    {
        var user = new UserModel("", "", "");
        _userRepository.Setup(r => r.FindUserByHashNickNameAsync("hash")).ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateUserAsync(user)).ThrowsAsync(new Exception("DB error"));

        var result = await _orchestrator.ConfirmEmailAsync("hash");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Error");
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldSucceed_WhenUserExists()
    {
        var user = new UserModel("login", "nick", "testImage");
        _userRepository.Setup(r => r.FindUserByHashNickNameAsync("hash")).ReturnsAsync(user);
        _userRepository.Setup(r => r.UpdateUserAsync(user)).Returns(Task.CompletedTask);

        var result = await _orchestrator.ConfirmEmailAsync("hash");

        result.Success.Should().BeTrue();
        result.Data.Should().Contain("confirm email");
        user.EmailConfirmed.Should().BeTrue();
    }
}
