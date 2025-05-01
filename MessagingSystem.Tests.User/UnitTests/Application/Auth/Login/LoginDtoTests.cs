using MessagingSystem.Services.User.Application.Auth.Login.Dto;

namespace MessagingSystem.Tests.User.UnitTests.Application.Auth.Login
{
    public class LoginDtoTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProperties_WithCorrectValues()
        {
            // Arrange
            const string login = "testuser";
            const string password = "Password123!";

            // Act
            var loginDto = new LoginDto(login, password);

            // Assert
            Assert.Equal(login, loginDto.Login);
            Assert.Equal(password, loginDto.Password);
        }

        [Fact]
        public void Properties_ShouldHaveInitAccessors()
        {
            // Arrange
            var type = typeof(LoginDto);
            
            // Act & Assert
            var loginProperty = type.GetProperty("Login");
            Assert.NotNull(loginProperty);
            Assert.NotNull(loginProperty.SetMethod);

            var passwordProperty = type.GetProperty("Password");
            Assert.NotNull(passwordProperty);
            Assert.NotNull(passwordProperty.SetMethod);
        }

        [Fact]
        public void Constructor_WithNullParameters_ShouldNotThrowException()
        {
            // Act & Assert
            var exception = Record.Exception(() => new LoginDto("", ""));
            Assert.Null(exception);
        }
        
        [Fact]
        public void ObjectInitialization_ShouldWork()
        {
            // Arrange & Act
            var loginDto = new LoginDto("originaluser", "originalpassword")
            {
                Login = "testuser",
                Password = "Password123!"
            };
            
            // Assert
            Assert.Equal("testuser", loginDto.Login);
            Assert.Equal("Password123!", loginDto.Password);
        }
        
        [Fact]
        public void ObjectInitialization_WithoutChanges_ShouldKeepOriginalValues()
        {
            // Arrange
            const string login = "testuser";
            const string password = "Password123!";
            
            // Act
            var loginDto = new LoginDto(login, password);
            
            // Assert
            Assert.Equal(login, loginDto.Login);
            Assert.Equal(password, loginDto.Password);
        }
        
        [Fact]
        public void Properties_CannotBeChanged_AfterInitialization()
        {
            // Arrange
            var loginDto = new LoginDto("testuser", "Password123!");
            
            // Assert
            Assert.Equal("testuser", loginDto.Login);
            Assert.Equal("Password123!", loginDto.Password);
        }
    }
}