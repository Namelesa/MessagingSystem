using Encryptor.Decryption;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Application.Auth.Login;

public class LoginUserOrchestratorTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IValidator<LoginDto>> _validatorMock = new();
    private readonly Mock<IDecryptionInfo> _decryptorMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IHasher> _hasherMock = new();

    private readonly LoginOrchestrator _orchestrator;

    public LoginUserOrchestratorTests()
    {
        _orchestrator = new LoginOrchestrator(
            _userRepoMock.Object,
            _validatorMock.Object,
            _decryptorMock.Object,
            _jwtServiceMock.Object,
            _hasherMock.Object
        );
    }

    [Fact]
    public async Task LoginUserAsync_ShouldReturnToken_WhenLoginSuccessful()
    {
        // Arrange
        var dto = new LoginDto("test", "pass", "pass");
        var user = new Services.User.Core.User.User("test", "nick", "test")
        {
            PasswordHash = "encrypted-password",
            EmailConfirmed = true
        };

        _validatorMock.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult());

        _hasherMock.Setup(x => x.Hash(dto.Login)).Returns("hashed-login");

        _userRepoMock.Setup(x => x.FindUserByHashLoginAsync("hashed-login"))
            .ReturnsAsync(user);

        _decryptorMock.Setup(x => x.Decrypt("encrypted-password"))
            .Returns("decrypted-password");

        _jwtServiceMock.Setup(x =>
                x.AuthenticateAndSetCookieAsync(dto, "decrypted-password"))
            .ReturnsAsync(true);

        // Act
        var result = await _orchestrator.LoginUserAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("True");
    }

    [Fact]
    public async Task LoginUserAsync_ShouldFail_WhenValidationFails()
    {
        // Arrange
        var dto = new LoginDto("", "", "");
        var failures = new ValidationResult(new List<ValidationFailure>
        {
            new("Login", "Login is required")
        });

        _validatorMock.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(failures);

        // Act
        var result = await _orchestrator.LoginUserAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Login is required");
    }

    [Fact]
    public async Task LoginUserAsync_ShouldFail_WhenUserNotFound()
    {
        var dto = new LoginDto("ghost", "any", "any");

        _validatorMock.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult());

        _hasherMock.Setup(x => x.Hash(dto.Login)).Returns("hash");

        _userRepoMock.Setup(x => x.FindUserByHashLoginAsync("hash"))
            .ReturnsAsync((Services.User.Core.User.User?)null);

        var result = await _orchestrator.LoginUserAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task LoginUserAsync_ShouldFail_WhenEmailNotConfirmed()
    {
        var dto = new LoginDto("user", "pass", "pass");
        var user = new Services.User.Core.User.User("user", "nick", "test") { EmailConfirmed = false };

        _validatorMock.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult());

        _hasherMock.Setup(x => x.Hash(dto.Login)).Returns("hash");

        _userRepoMock.Setup(x => x.FindUserByHashLoginAsync("hash"))
            .ReturnsAsync(user);

        var result = await _orchestrator.LoginUserAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("confirm email");
    }

    [Fact]
    public async Task LoginUserAsync_ShouldReturnFalse_WhenAuthenticationFails()
    {
        var dto = new LoginDto("user", "wrong-pass", "wrong-nick");
        var user = new Services.User.Core.User.User("user", "nick", "test")
        {
            PasswordHash = "some-encrypted-hash",
            EmailConfirmed = true
        };

        _validatorMock.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult());

        _hasherMock.Setup(x => x.Hash(dto.Login)).Returns("hash");

        _userRepoMock.Setup(x => x.FindUserByHashLoginAsync("hash"))
            .ReturnsAsync(user);

        _decryptorMock.Setup(x => x.Decrypt("some-encrypted-hash"))
            .Returns("decrypted-pass");

        _jwtServiceMock.Setup(x => x.AuthenticateAndSetCookieAsync(dto, "decrypted-pass"))
            .ReturnsAsync(false);

        var result = await _orchestrator.LoginUserAsync(dto);

        result.Success.Should().BeTrue();
        result.Data.Should().Be("False");
    }
    [Fact]
    public async Task LoginUserAsync_ShouldFail_WhenNickNameDoesNotMatch()
    {
        // Arrange
        var dto = new LoginDto("user", "pass", "wrong-nick");
        var user = new Services.User.Core.User.User("user", "nick", "test")
        {
            PasswordHash = "encrypted-password",
            EmailConfirmed = true,
        };

        _validatorMock.Setup(x => x.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult());

        _hasherMock.Setup(x => x.Hash(dto.Login)).Returns("hashed-login");
        _hasherMock.Setup(x => x.Hash(dto.NickName)).Returns("wrong-nick-hash");

        _userRepoMock.Setup(x => x.FindUserByHashLoginAsync("hashed-login"))
            .ReturnsAsync(user);

        // Act
        var result = await _orchestrator.LoginUserAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Input your real nick name");
    }
}