using MessagingSystem.Services.User.Infrastructure.PasswordHasher;

namespace MessagingSystem.Tests.User.UnitTests.Infrastructure
{
    public class PasswordHasherTests
    {
        private readonly HasherPassword _hasher = new();

        [Fact]
        public void Hash_ShouldReturnHashedPassword()
        {
            // Arrange
            const string password = "SuperSecure123!";

            // Act
            var hashed = _hasher.Hash(password);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(hashed));
            Assert.NotEqual(password, hashed);
        }

        [Fact]
        public void Verify_ShouldReturnTrue_ForCorrectPassword()
        {
            // Arrange
            const string password = "MyPassword123!";
            var hashed = _hasher.Hash(password);

            // Act
            var isVerified = _hasher.Verify(hashed, password);

            // Assert
            Assert.True(isVerified);
        }

        [Fact]
        public void Verify_ShouldReturnFalse_ForIncorrectPassword()
        {
            // Arrange
            const string password = "OriginalPassword";
            const string wrongPassword = "WrongPassword";
            var hashed = _hasher.Hash(password);

            // Act
            var isVerified = _hasher.Verify(hashed, wrongPassword);

            // Assert
            Assert.False(isVerified);
        }

        [Fact]
        public void HashingSamePassword_Twice_ShouldProduceDifferentHashes()
        {
            // Arrange
            const string password = "SamePassword";

            // Act
            var hash1 = _hasher.Hash(password);
            var hash2 = _hasher.Hash(password);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Verify_ShouldReturnFalse_IfHashIsInvalid()
        {
            // Arrange
            const string password = "SomePassword";
            const string fakeHash = "ThisIsNotAValidHash==";

            // Act
            var result = _hasher.Verify(fakeHash, password);

            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void Hash_ShouldReturnNull_ForEmptyPassword()
        {
            // Arrange
            const string password = "";

            // Act
            var hashed = _hasher.Hash(password);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(hashed));
        }

        [Fact]
        public void Verify_ShouldReturnFalse_ForEmptyHash()
        {
            // Arrange
            const string password = "AnyPassword";
            const string emptyHash = "";

            // Act
            var result = _hasher.Verify(emptyHash, password);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Verify_ShouldReturnFalse_ForNullPassword()
        {
            // Arrange
            const string hash = "AnyValidHash";
            string? nullPassword = null;

            // Act
            var result = _hasher.Verify(hash, nullPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Verify_ShouldReturnFalse_ForNullHash()
        {
            // Arrange
            const string password = "AnyPassword";
            string? nullHash = null;

            // Act
            var result = _hasher.Verify(nullHash, password);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Verify_ShouldReturnFalse_IfFormatExceptionOccurs()
        {
            // Arrange
            const string password = "Password";
            const string invalidHash = "invalidHash";

            // Act
            var result = _hasher.Verify(invalidHash, password);

            // Assert
            Assert.False(result);
        }
    }
}
