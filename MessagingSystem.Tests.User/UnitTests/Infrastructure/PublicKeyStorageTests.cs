using MessagingSystem.Services.User.Infrastructure.Keys;

namespace MessagingSystem.Tests.User.UnitTests.Infrastructure
{
    public class PublicKeyStorageTests
    {
        [Fact]
        public void Save_ShouldStoreKey()
        {
            // Arrange
            var storage = new PublicKeyStorage();
            const string serviceName = "Auth";
            const string publicKey = "some-public-key";

            // Act
            storage.Save(serviceName, publicKey);
            var result = storage.Get(serviceName);

            // Assert
            Assert.Equal(publicKey, result);
        }

        [Fact]
        public void Get_ShouldReturnNull_WhenKeyDoesNotExist()
        {
            // Arrange
            var storage = new PublicKeyStorage();

            // Act
            var result = storage.Get("NonExistingService");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Save_ShouldOverwriteExistingKey()
        {
            // Arrange
            var storage = new PublicKeyStorage();
            const string serviceName = "Payment";
            const string oldKey = "old-key";
            const string newKey = "new-key";

            // Act
            storage.Save(serviceName, oldKey);
            storage.Save(serviceName, newKey);
            var result = storage.Get(serviceName);

            // Assert
            Assert.Equal(newKey, result);
        }
    }
}