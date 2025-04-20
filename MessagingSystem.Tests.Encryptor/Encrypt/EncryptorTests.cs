using Encryptor.Encryption;
using Microsoft.Extensions.Configuration;

namespace MessagingSystem.Tests.Encryptor.Encrypt;

public class EncryptionInfoTests
{
    private readonly string _testKey = "test-secret-key";
    private readonly IConfiguration _config;

    public EncryptionInfoTests()
    {
        var configData = new Dictionary<string, string>
        {
            { "Encryption:ChaChaKey", _testKey }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    [Fact]
    public void GetPublicKey_Throws_WhenFileNotFound()
    {
        // Arrange
        var tempDir = Path.Combine(AppContext.BaseDirectory, "Keys");
        var publicKeyPath = Path.Combine(tempDir, "public.key");
        var privateKeyPath = Path.Combine(tempDir, "private.key");
        
        if (File.Exists(publicKeyPath)) File.Delete(publicKeyPath);
        if (File.Exists(privateKeyPath)) File.Delete(privateKeyPath);
        
        var info = new EncryptionInfo(_config);
        
        if (File.Exists(publicKeyPath)) File.Delete(publicKeyPath);

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => info.GetPublicKey());
        Assert.Equal("Public key not found", ex.Message);
    }

    
    [Fact]
    public void Constructor_Throws_WhenKeyIsMissing()
    {
        var config = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new EncryptionInfo(config));
        Assert.Equal("Encryption key not configured", ex.Message);
    }

    [Fact]
    public void Encrypt_ReturnsEncryptedData()
    {
        // Arrange
        var info = new EncryptionInfo(_config);
        var plainText = "Hello world";

        // Act
        var encrypted = info.Encrypt(plainText);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(encrypted));
        Assert.NotEqual(plainText, encrypted);
    }

    [Fact]
    public void EncryptRsa_Encrypts_WithPublicKey()
    {
        // Arrange
        var info = new EncryptionInfo(_config);
        var publicKey = info.GetPublicKey();
        var plainText = "test";

        // Act
        var encrypted = info.EncryptRsa(plainText, publicKey);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(encrypted));
    }

    private class TestUser
    {
        public string? UserName { get; init; }
        public string? Login { get; init; }
        public string? Email { get; init; }
        public string? NickName { get; init; }
    }

    [Fact]
    public void EncryptObjectStringsForUpdate_EncryptsExcludedFieldsOnly()
    {
        // Arrange
        var info = new EncryptionInfo(_config);
        var user = new TestUser
        {
            UserName = "test-user",
            Email = "test@mail.com",
            NickName = "nick",
            Login = "log"
        };

        // Act
        info.EncryptObjectStringsForUpdate(user);

        // Assert
        Assert.NotEqual("test-user", user.UserName);
        Assert.NotEqual("test@mail.com", user.Email);
        Assert.NotEqual("log", user.Login);
        Assert.NotEqual("nick", user.NickName);
    }

    private class TestAccount
    {
        public string? NickName { get; init; }
        public string? Email { get; init; }
        public string? Login { get; init; }
    }

    [Fact]
    public void EncryptRsaObjectStrings_EncryptsExceptNickName()
    {
        // Arrange
        var info = new EncryptionInfo(_config);
        var publicKey = info.GetPublicKey();
        var obj = new TestAccount
        {
            NickName = "nick",
            Email = "test@mail.com",
            Login = "log"
        };

        // Act
        info.EncryptRsaObjectStrings(obj, publicKey);

        // Assert
        Assert.Equal("nick", obj.NickName);
        Assert.NotEqual("test@mail.com", obj.Email);
        Assert.NotEqual("log", obj.Login);
    }

    private class TestProfile
    {
        public string? HashLogin { get; init; }
        public string? Email { get; init; }
        public string? Login { get; init; }
    }

    [Fact]
    public void EncryptObjectStrings_EncryptsAllExceptHashes()
    {
        // Arrange
        var info = new EncryptionInfo(_config);
        var obj = new TestProfile
        {
            HashLogin = "hashed",
            Email = "mail@test.com",
            Login = "login"
        };

        // Act
        info.EncryptObjectStrings(obj);

        // Assert
        Assert.Equal("hashed", obj.HashLogin);
        Assert.NotEqual("mail@test.com", obj.Email);
        Assert.NotEqual("login", obj.Login);
    }

    [Fact]
    public void GetPublicKey_ReturnsValidBase64()
    {
        // Arrange
        var info = new EncryptionInfo(_config);

        // Act
        var publicKey = info.GetPublicKey();

        // Assert
        var bytes = Convert.FromBase64String(publicKey);
        Assert.True(bytes.Length > 0);
    }
}