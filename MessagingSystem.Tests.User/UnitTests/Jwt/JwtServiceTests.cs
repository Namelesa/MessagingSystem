using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Encryptor.Encryption;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Jwt;

public class JwtServiceTests
{
    private readonly Mock<ILogger<JwtService>> _mockLogger = new();
    private readonly Mock<IEncryptionInfo> _mockEncryptor = new();
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor = new();
    private readonly Mock<PasswordHasher<LoginDto>> _mockPasswordHasher = new();

    private JwtService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWTConfig:Issuer"] = "testIssuer",
                ["JWTConfig:Audience"] = "testAudience",
                ["JWTConfig:Key"] = "super_secret_key_1234567890!ABCD",
                ["JWTConfig:TokenValidityMinutes"] = "60"
            })
            .Build();

        return new JwtService(
            config,
            _mockLogger.Object,
            _mockEncryptor.Object,
            _mockHttpContextAccessor.Object);
    }
        

    // ValidateUserCredentials
    [Fact]
    public void ValidateUserCredentials_UserPasswordIsNull_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var user = new LoginDto("test", null, null);

        // Act
        var result = InvokeValidateUserCredentials(service, user, "anyPassword");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateUserCredentials_PasswordDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        
        var correctHashedPassword = "AQAAAAIAAYagAAAAEKaQL0wve0+cWn37VV/GpD7Ie9LnIJvx4NxiGGF7HJ9FSdosaL5jvWW59hF4FspeDQ==";
        
        _mockPasswordHasher.Setup(m => m.VerifyHashedPassword(It.IsAny<LoginDto>(), It.IsAny<string>(), correctHashedPassword))
            .Returns(PasswordVerificationResult.Failed);

        var user = new LoginDto("test", correctHashedPassword, "Pass1@");
        
        var incorrectPassword = "AQAAAAIAAYagAAAAEKaQL0wve0+cWn37VV/GpD7Ie9LnIJvx4NxiGGF7HJ9FSdosaL5jvWW59hF4FspeDC==";

        // Act
        var result = InvokeValidateUserCredentials(service, user, incorrectPassword);

        // Assert
        Assert.False(result); 
    }
    
    // AuthenticateAndSetCookieAsync
    [Fact]
    public async Task AuthenticateAndSetCookieAsync_ShouldSetCookie_WhenAuthenticationSucceeds()
    {
        // Arrange
        var token = "mockedToken";
        var passwordRequest = "Test123!4987654";
        var user = new LoginDto("user", passwordRequest, "Pass1@");
        
        var passwordHasher = new PasswordHasher<LoginDto>();
        var hashedPassword = passwordHasher.HashPassword(user, passwordRequest); 

        var mockResponse = new Mock<HttpResponse>();
        var mockCookies = new Mock<IResponseCookies>();
        mockResponse.SetupGet(r => r.Cookies).Returns(mockCookies.Object);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.SetupGet(c => c.Response).Returns(mockResponse.Object);
        _mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(mockHttpContext.Object);

        SetupPasswordHasher(PasswordVerificationResult.Success);
        _mockEncryptor.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(token);

        var service = CreateService();

        // Act
        var result = await service.AuthenticateAndSetCookieAsync(user, hashedPassword);

        // Assert
        Assert.True(result);

        mockCookies.Verify(c => c.Append(
            "access_token",
            token,
            It.Is<CookieOptions>(opt =>
                opt.HttpOnly &&
                opt.Secure &&
                opt.SameSite == SameSiteMode.None &&
                opt.Expires.HasValue)), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAndSetCookieAsync_ShouldReturnFalse_WhenAuthenticationFails()
    {
        // Arrange
        var passwordRequest = "Test123!4987654";
        var user = new LoginDto("user", passwordRequest, "Pass1@");
        
        var passwordHasher = new PasswordHasher<LoginDto>();
        var hashedPassword = passwordHasher.HashPassword(user, passwordRequest); 

        SetupPasswordHasher(PasswordVerificationResult.Failed);
        _mockEncryptor.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string?)null);

        var service = CreateService();

        // Act
        var result = await service.AuthenticateAndSetCookieAsync(user, hashedPassword);

        // Assert
        Assert.False(result);

        _mockHttpContextAccessor.Verify(x => x.HttpContext, Times.Never);
    }

    
    // AuthenticateAsync
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnToken_WhenPasswordIsCorrect()
    {
        var passwordRequest = "Test123!4987654";
        var user = new LoginDto("user", passwordRequest, "Pass1@");
        
        var passwordHasher = new PasswordHasher<LoginDto>();
        var hashedPassword = passwordHasher.HashPassword(user, passwordRequest); 

        var expectedToken = "encryptedToken123";
        
        SetupEncryptor(expectedToken);
        
        var service = CreateService();

        // Act
        var token = await AuthenticateAsyncInternal(service, user, hashedPassword);

        // Assert
        Assert.Equal(expectedToken, token);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
    {
        // Arrange
        var passwordRequest = "AQAAAAIAAYagAAAAEKaQL0wve0+cWn37VV/GpD7Ie9LnIJvx4NxiGGF7HJ9FSdosaL5jvWW59hF4FspeDW==";
        var passwordVerificationResult = PasswordVerificationResult.Failed;
        string? expectedToken = null;
        var user = CreateUser();
        SetupPasswordHasher(passwordVerificationResult);
        SetupEncryptor(expectedToken);

        var service = CreateService();

        // Act
        var token = await AuthenticateAsyncInternal(service, user, passwordRequest);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenEncryptionFails()
    {
        // Arrange
        var passwordRequest = "AQAAAAIAAYagAAAAEKaQL0wve0+cWn37VV/GpD7Ie9LnIJvx4NxiGGF7HJ9FSdosaL5jvWW59hF4FspeDQ==";
        var passwordVerificationResult = PasswordVerificationResult.Success;
        string? expectedToken = null;
        var user = CreateUser();
        SetupPasswordHasher(passwordVerificationResult);
        SetupEncryptor(expectedToken);

        var service = CreateService();

        // Act
        var token = await AuthenticateAsyncInternal(service, user, passwordRequest);

        // Assert
        Assert.Null(token);
    }
    
    [Fact]
    public async Task AuthenticateAsync_ShouldLogWarning_WhenCredentialsAreInvalid()
    {
        // Arrange
        var user = new LoginDto("testUser", null, null);
        var service = CreateService();

        // Act
        var result = await AuthenticateAsyncInternal(service, user, "irrelevant");

        // Assert
        Assert.Null(result);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Authentication failed: invalid credentials")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNulls_WhenEncryptionFails()
    {
        // Arrange
        var passwordRequest = "Test123!4987654";
        var user = new LoginDto("testUser", passwordRequest, "Pass1@");

        var passwordHasher = new PasswordHasher<LoginDto>();
        var hashedPassword = passwordHasher.HashPassword(user, passwordRequest);

        var expectedException = new Exception("Encryption failed");

        _mockEncryptor
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Throws(expectedException);

        var service = CreateService();

        // Act
        var token = await AuthenticateAsyncInternal(service, user, hashedPassword);

        // Assert
        Assert.Null(token);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Error occurred while generating JWT token")),
                It.Is<Exception>(ex => ex.Message == "Encryption failed"),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    
    // GenerateJwtToken
    [Fact]
    public void GenerateJwtToken_ShouldGenerateToken_WhenConfigIsValid()
    {
        // Arrange
        var configWithKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWTConfig:Issuer", "issuer" },
                { "JWTConfig:Audience", "audience" },
                { "JWTConfig:Key", "super_secret_key_1234567890!ABCD" },
                { "JWTConfig:TokenValidityMinutes", "60" }
            })
            .Build();

        var service = new JwtService(
            configWithKey,
            _mockLogger.Object,
            _mockEncryptor.Object,
            new Mock<IHttpContextAccessor>().Object);;

        var loginDto = new LoginDto("user", "password", "Pass1@");

        var method = typeof(JwtService).GetMethod("GenerateJwtToken", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var token = (string)method!.Invoke(service, new object[] { loginDto })!;

        // Assert
        Assert.NotNull(token);
        Assert.IsType<string>(token);
    
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("issuer", jwtToken.Issuer);
        Assert.Equal("audience", jwtToken.Audiences.First());
    }
    
    [Fact]
    public void GenerateJwtToken_ShouldThrow_WhenKeyIsMissingCompletely()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWTConfig:Issuer", "issuer" },
                { "JWTConfig:Audience", "audience" },
                { "JWTConfig:TokenValidityMinutes", "60" }
            })
            .Build();

        var service = new JwtService(config, _mockLogger.Object, _mockEncryptor.Object, new Mock<IHttpContextAccessor>().Object);

        var method = typeof(JwtService).GetMethod("GenerateJwtToken", BindingFlags.NonPublic | BindingFlags.Instance);

        var ex = Assert.Throws<TargetInvocationException>(() =>
            method!.Invoke(service, [new LoginDto("user", "pass", "Pass1@")]));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("JWT key is not configured properly.", ex.InnerException!.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateJwtToken_ShouldThrow_WhenKeyIsEmptyOrWhitespace(string invalidKey)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWTConfig:Issuer", "issuer" },
                { "JWTConfig:Audience", "audience" },
                { "JWTConfig:TokenValidityMinutes", "60" },
                { "JWTConfig:Key", invalidKey }
            })
            .Build();

        var service = new JwtService(config, _mockLogger.Object, _mockEncryptor.Object, new Mock<IHttpContextAccessor>().Object);

        var method = typeof(JwtService).GetMethod("GenerateJwtToken", BindingFlags.NonPublic | BindingFlags.Instance);

        var ex = Assert.Throws<TargetInvocationException>(() =>
            method!.Invoke(service, [new LoginDto("user", "pass", "Pass1@")]));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("JWT key is not configured properly.", ex.InnerException!.Message);
    }
    
    private static bool InvokeValidateUserCredentials(JwtService service, LoginDto? user, string password)
    {
        var method = typeof(JwtService).GetMethod("ValidateUserCredentials", BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool)method!.Invoke(service, [user, password])!;
    }
    
    private LoginDto CreateUser()
    {
        return new LoginDto("testUser", "AQAAAAIAAYagAAAAEKaQL0wve0+cWn37VV/GpD7Ie9LnIJvx4NxiGGF7HJ9FSdosaL5jvWW59hF4FspeDQ==", "Pass1@");
    }

    private void SetupPasswordHasher(PasswordVerificationResult passwordVerificationResult)
    {
        _mockPasswordHasher
            .Setup(m => m.VerifyHashedPassword(It.IsAny<LoginDto>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(passwordVerificationResult);
    }

    private void SetupEncryptor(string? expectedToken)
    {
        if (expectedToken != null)
        {
            _mockEncryptor.Setup(m => m.Encrypt(It.IsAny<string>())).Returns(expectedToken);
        }
        else
        {
            _mockEncryptor.Setup(m => m.Encrypt(It.IsAny<string>())).Throws(new Exception("Encryption failed"));
        }
    }

    private async Task<string?> AuthenticateAsyncInternal(JwtService service, LoginDto user, string passwordRequest)
    {
        var method = typeof(JwtService).GetMethod("AuthenticateAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        return await (Task<string?>)method!.Invoke(service, new object[] { user, passwordRequest });
    }
}
