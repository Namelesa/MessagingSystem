using System.Security.Cryptography;
using Encryptor.Publisher;

namespace MessagingSystem.Tests.Encryptor.Publisher
{
    public class RsaKeyPairTests : IDisposable
    {
        private readonly string _testPublicKeyPath = Path.Combine(Path.GetTempPath(), $"test_public_key_{Guid.NewGuid()}.txt");
        private readonly string _testPrivateKeyPath = Path.Combine(Path.GetTempPath(), $"test_private_key_{Guid.NewGuid()}.txt");

        // Create temporary file paths for testing

        public void Dispose()
        {
            // Clean up test files after each test
            if (File.Exists(_testPublicKeyPath))
                File.Delete(_testPublicKeyPath);
            
            if (File.Exists(_testPrivateKeyPath))
                File.Delete(_testPrivateKeyPath);
        }

        [Fact]
        public void Generate_ShouldCreateValidKeyPair()
        {
            // Act
            var keyPair = RsaKeyPair.Generate();

            // Assert
            Assert.NotNull(keyPair);
            Assert.NotEmpty(keyPair.PrivateKey);
    
            // Use reflection to access the private PublicKey property
            var publicKeyProperty = typeof(RsaKeyPair).GetProperty("PublicKey", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            var publicKey = (string)publicKeyProperty.GetValue(keyPair);
            Assert.NotEmpty(publicKey);
    
            // Verify the keys are valid by trying to load them into RSA
            using var rsa = RSA.Create();
    
            // This will throw an exception if the private key format is invalid
            var privateKeyBytes = Convert.FromBase64String(keyPair.PrivateKey);
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
    
            // We should also verify the public key
            var publicKeyBytes = Convert.FromBase64String(publicKey);
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        }
        
        [Fact]
        public void SaveToFiles_ShouldPersistKeysToFiles()
        {
            // Arrange
            var keyPair = RsaKeyPair.Generate();

            // Act
            RsaKeyPair.SaveToFiles(keyPair, _testPublicKeyPath, _testPrivateKeyPath);

            // Assert
            Assert.True(File.Exists(_testPublicKeyPath));
            Assert.True(File.Exists(_testPrivateKeyPath));
            
            // Check content
            var savedPublicKey = File.ReadAllText(_testPublicKeyPath);
            var savedPrivateKey = File.ReadAllText(_testPrivateKeyPath);
            
            Assert.Equal(keyPair.PrivateKey, savedPrivateKey);
        }

        [Fact]
        public void LoadFromFiles_WithValidFiles_ShouldLoadKeyPair()
        {
            // Arrange
            var originalKeyPair = RsaKeyPair.Generate();
            RsaKeyPair.SaveToFiles(originalKeyPair, _testPublicKeyPath, _testPrivateKeyPath);

            // Act
            var loadedKeyPair = RsaKeyPair.LoadFromFiles(_testPublicKeyPath, _testPrivateKeyPath);

            // Assert
            Assert.NotNull(loadedKeyPair);
            Assert.Equal(originalKeyPair.PrivateKey, loadedKeyPair.PrivateKey);
        }

        [Fact]
        public void LoadFromFiles_WithNonExistentFiles_ShouldThrowFileNotFoundException()
        {
            // Arrange
            string nonExistentPublicKeyPath = Path.Combine(Path.GetTempPath(), "non_existent_public.key");
            string nonExistentPrivateKeyPath = Path.Combine(Path.GetTempPath(), "non_existent_private.key");

            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() => 
                RsaKeyPair.LoadFromFiles(nonExistentPublicKeyPath, nonExistentPrivateKeyPath));
            
            Assert.Equal("Rsa not founded", exception.Message);
        }

        [Fact]
        public void LoadFromFiles_WithOnlyPublicKeyMissing_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var keyPair = RsaKeyPair.Generate();
            File.WriteAllText(_testPrivateKeyPath, keyPair.PrivateKey);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => 
                RsaKeyPair.LoadFromFiles(_testPublicKeyPath, _testPrivateKeyPath));
        }

        [Fact]
        public void LoadFromFiles_WithOnlyPrivateKeyMissing_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var keyPair = RsaKeyPair.Generate();
            File.WriteAllText(_testPublicKeyPath, keyPair.PrivateKey);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => 
                RsaKeyPair.LoadFromFiles(_testPublicKeyPath, _testPrivateKeyPath));
        }
    }
}