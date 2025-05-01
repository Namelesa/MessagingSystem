using MessagingSystem.Services.User.Infrastructure.HasherInfo;

namespace MessagingSystem.Tests.User.UnitTests.Infrastructure
{
    public class HasherTests
    {
        private readonly Hasher _hasher = new();

        [Fact]
        public void Hash_ShouldReturnBase64String()
        {
            // Arrange
            const string input = "TestPassword";

            // Act
            var result = _hasher.Hash(input);

            // Assert
            var decodedBytes = Convert.FromBase64String(result);
            Assert.Equal(32, decodedBytes.Length);
        }

        [Fact]
        public void Hash_ShouldBeDeterministic()
        {
            // Arrange
            const string input = "SomePassword";

            // Act
            var hash1 = _hasher.Hash(input);
            var hash2 = _hasher.Hash(input);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void Hash_ShouldIgnoreCase()
        {
            // Arrange
            const string lower = "mypassword";
            const string upper = "MYPASSWORD";

            // Act
            var hashLower = _hasher.Hash(lower);
            var hashUpper = _hasher.Hash(upper);

            // Assert
            Assert.Equal(hashLower, hashUpper);
        }

        [Fact]
        public void Hash_DifferentInputsShouldProduceDifferentHashes()
        {
            // Arrange
            const string input1 = "password1";
            const string input2 = "password2";

            // Act
            var hash1 = _hasher.Hash(input1);
            var hash2 = _hasher.Hash(input2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Hash_EmptyString_ShouldReturnValidBase64()
        {
            // Arrange
            const string input = "";

            // Act
            var hash = _hasher.Hash(input);

            // Assert
            var decoded = Convert.FromBase64String(hash);
            Assert.Equal(32, decoded.Length);
        }
    }
}
