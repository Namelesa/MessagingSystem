using Encryptor.Encryption;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Infrastructure;

    public class JwtServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<JwtService>> _mockLogger;
        private readonly Mock<IEncryptionInfo> _mockEncryptor;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly JwtService _jwtService;
        private readonly DefaultHttpContext _httpContext;

        public JwtServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<JwtService>>();
            _mockEncryptor = new Mock<IEncryptionInfo>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            
            _mockConfig.Setup(c => c["JWTConfig:Key"]).Returns("very_secret_key_12345");
            _mockConfig.Setup(c => c["JWTConfig:Issuer"]).Returns("test_issuer");
            _mockConfig.Setup(c => c["JWTConfig:Audience"]).Returns("test_audience");

            // TokenValidityMinutes как IConfigurationSection
            var tokenValiditySection = new Mock<IConfigurationSection>();
            tokenValiditySection.Setup(s => s.Value).Returns("15");
            _mockConfig.Setup(c => c.GetSection("JWTConfig:TokenValidityMinutes")).Returns(tokenValiditySection.Object);
            
            _httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(_httpContext);
            
            _jwtService = new JwtService(
                _mockConfig.Object,
                _mockLogger.Object,
                _mockEncryptor.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task AuthenticateAndSetCookieAsync_ShouldReturnFalse_WhenUserIsNull()
        {
            // Act
            var result = await _jwtService.AuthenticateAndSetCookieAsync(null, "password");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AuthenticateAndSetCookieAsync_ShouldReturnFalse_WhenPasswordIncorrect()
        {
            // Arrange
            var user = new LoginDto("test_user", "");
            var hasher = new PasswordHasher<LoginDto>();
            
            var hashedPassword = hasher.HashPassword(user, "correct_password");
            var loginDto = new LoginDto("test_user", hashedPassword); 
            
            var passwordRequest = "wrong_password"; 
            
            var result = hasher.VerifyHashedPassword(user, loginDto.Password, passwordRequest);

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }
    }

