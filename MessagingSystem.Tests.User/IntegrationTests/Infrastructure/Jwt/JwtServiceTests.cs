using System.Reflection;
using Encryptor.Encryption;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessagingSystem.Tests.User.IntegrationTests.Infrastructure.Jwt;

public class JwtServiceIntegrationTests
{
    private readonly Mock<IEncryptionInfo> _encryptInfoMock;
    private readonly Mock<IHasherPassword> _passwordHasherMock;
    private readonly Mock<ILogger<JwtService>> _loggerMock;
    private readonly JwtService _jwtService;

    public JwtServiceIntegrationTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JWTConfig:Issuer", "test-issuer" },
            { "JWTConfig:Audience", "test-audience" },
            { "JWTConfig:Key", Convert.ToBase64String("super_secret_key_1234567890"u8.ToArray()) },
            { "JWTConfig:TokenValidityMinutes", "60" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _encryptInfoMock = new Mock<IEncryptionInfo>();
        _loggerMock = new Mock<ILogger<JwtService>>();
        _passwordHasherMock = new Mock<IHasherPassword>();

        var httpContext = new DefaultHttpContext();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        _jwtService = new JwtService(
            config,
            _loggerMock.Object,
            _encryptInfoMock.Object,
            httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task AuthenticateAndSetCookieAsync_ShouldReturnFalse_WhenCredentialsAreInValid()
    {
        // Arrange
        var password = "correctPassword";
        var loginDto = CreateUserWithHashPassword("testUser", password); // невалидный

        _encryptInfoMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns("encrypted-token");

        // Act
        var result = await _jwtService.AuthenticateAndSetCookieAsync(loginDto, password);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthenticateAndSetCookieAsync_ShouldReturnTrue_WhenCredentialsAreValid()
    {
        // Arrange
        var password = "correctPassword";
        var loginDto = CreateUserWithHashPassword("testUser", password);

        _encryptInfoMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns("encrypted-token");

        // Act
        var result = await _jwtService.AuthenticateAndSetCookieAsync(loginDto, password);

        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenTokenEncryptionFails()
    {
        // Arrange
        var password = "validPassword";
        var loginDto = CreateUserWithHashPassword("testUser", password);

        _encryptInfoMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Throws(new Exception("encryption error"));

        var method = typeof(JwtService).GetMethod("AuthenticateAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<string?>)method!.Invoke(_jwtService, [loginDto, password])!;
        var result = await task;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateJwtToken_ShouldThrow_WhenKeyIsMissing()
    {
        // Arrange
        var configWithoutKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWTConfig:Issuer", "issuer" },
                { "JWTConfig:Audience", "audience" },
                { "JWTConfig:TokenValidityMinutes", "60" }
            })
            .Build();

        var jwtService = new JwtService(
            configWithoutKey,
            _loggerMock.Object,
            _encryptInfoMock.Object,
            new Mock<IHttpContextAccessor>().Object);

        var loginDto = new LoginDto("user", "password");

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() =>
        {
            var method = typeof(JwtService).GetMethod("GenerateJwtToken", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(jwtService, [loginDto]);
        });

        Assert.Contains("Exception has been thrown by the target", ex.Message);
    }

    [Fact]
    public void ValidateUserCredentials_ShouldReturnFalse_WhenPasswordIsNull()
    {
        // Arrange
        var loginDto = new LoginDto("user", null!);

        var method = typeof(JwtService).GetMethod("ValidateUserCredentials", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method!.Invoke(_jwtService, [loginDto, "anyPassword"])!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserCredentials_ShouldReturnFalse_WhenUserPasswordIsNull()
    {
        // Arrange
        var loginDto = new LoginDto("testUser", null!);

        // Act
        var method = typeof(JwtService).GetMethod("ValidateUserCredentials", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method!.Invoke(_jwtService, [loginDto, "anyPassword"])!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserCredentials_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var password = "correctPassword";
        var loginDto = CreateUserWithHashPassword("testUser", password);

        // Act
        var method = typeof(JwtService).GetMethod("ValidateUserCredentials", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method!.Invoke(_jwtService, [loginDto, "wrongPassword"])!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserCredentials_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "correctPassword";
        var loginDto = CreateUserWithHashPassword("testUser", password);

        // Act
        var method = typeof(JwtService).GetMethod("ValidateUserCredentials", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method!.Invoke(_jwtService, [loginDto, password])!;

        // Assert
        Assert.False(result);
    }

    private LoginDto CreateUserWithHashPassword(string login, string password)
    {
        var hashedPassword = _passwordHasherMock.Object.Hash(password);
        return new LoginDto(login, hashedPassword); 
    }
}
