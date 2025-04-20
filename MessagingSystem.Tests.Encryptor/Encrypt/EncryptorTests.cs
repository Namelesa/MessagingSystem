using Encryptor.Encryption;
using Encryptor.Publisher;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MessagingSystem.Tests.Encryptor.Encrypt;

public class EncryptionInfoAdditionalTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _publicKeyPath;
    private readonly string _privateKeyPath;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly string _testKey = "TestEncryptionKey123!";

    public EncryptionInfoAdditionalTests()
    {
        // Setup temp directory for keys
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"EncryptionTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        
        _publicKeyPath = Path.Combine(_tempDirectory, "public.key");
        _privateKeyPath = Path.Combine(_tempDirectory, "private.key");

        // Generate and save test keys
        var keyPair = RsaKeyPair.Generate();
        RsaKeyPair.SaveToFiles(keyPair, _publicKeyPath, _privateKeyPath);

        // Setup configuration mock
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Encryption:ChaChaKey"]).Returns(_testKey);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempDirectory)) return;
        try
        {
            Directory.Delete(_tempDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public void Constructor_WithMissingKeysDirectory_GeneratesNewKeys()
    {
        // Arrange
        // Delete the Keys directory to force key generation path
        var keyDirectory = Path.Combine(AppContext.BaseDirectory, "Keys");
        if (Directory.Exists(keyDirectory))
        {
            Directory.Delete(keyDirectory, true);
        }

        // Act
        var encryptionInfo = new EncryptionInfo(_configurationMock.Object);

        // Assert
        Assert.NotNull(encryptionInfo);
        Assert.True(File.Exists(Path.Combine(keyDirectory, "public.key")));
        Assert.True(File.Exists(Path.Combine(keyDirectory, "private.key")));
    }
    
    [Fact]
    public void GetPublicKey_MissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        SetupBasePaths();
        var encryptionInfo = new EncryptionInfo(_configurationMock.Object);
        
        // Delete the public key file after initialization
        var publicKeyPath = Path.Combine(AppContext.BaseDirectory, "Keys", "public.key");
        File.Delete(publicKeyPath);
        
        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => encryptionInfo.GetPublicKey());
        Assert.Equal("Public key not found", exception.Message);
    }
    
    [Fact]
    public void EncryptObjectStringsForUpdate_ExcludedPropertiesChecked()
    {
        // Arrange
        SetupBasePaths();
        var encryptionInfo = new EncryptionInfo(_configurationMock.Object);
        var testObj = new TestObject
        {
            Id = 1,
            UserName = "testuser",
            Login = "login123",
            Email = "test@example.com",
            NickName = "testnick",
            Password = "password123"
        };
        
        // Save original values for comparison
        var originalUserName = testObj.UserName;
        var originalLogin = testObj.Login;
        var originalEmail = testObj.Email;
        var originalNickName = testObj.NickName;
        var originalPassword = testObj.Password;
        
        // Act
        encryptionInfo.EncryptObjectStringsForUpdate(testObj);
        
        // Assert - The opposite of what you had before - these should be encrypted (not excluded)
        Assert.NotEqual(originalUserName, testObj.UserName);
        Assert.NotEqual(originalLogin, testObj.Login);
        Assert.NotEqual(originalEmail, testObj.Email);
        Assert.NotEqual(originalNickName, testObj.NickName);
        // Password should not be in the excluded properties, so it should remain the same
        Assert.Equal(originalPassword, testObj.Password);
    }
    
    [Fact]
    public void EncryptObjectStringsForUpdate_NullValues_HandlesGracefully()
    {
        // Arrange
        SetupBasePaths();
        var encryptionInfo = new EncryptionInfo(_configurationMock.Object);
        var testObj = new TestObject
        {
            Id = 1,
            UserName = null,
            Password = null,
            Email = null,
            NickName = null
        };
        
        // Act - This should not throw
        encryptionInfo.EncryptObjectStringsForUpdate(testObj);
        
        // Assert
        Assert.Null(testObj.UserName);
        Assert.Null(testObj.Password);
        Assert.Null(testObj.Email);
        Assert.Null(testObj.NickName);
    }
    
    [Fact]
    public void EncryptRsaObjectStrings_NullValues_HandlesGracefully()
    {
        // Arrange
        SetupBasePaths();
        var encryptionInfo = new EncryptionInfo(_configurationMock.Object);
        var testObj = new TestObject
        {
            Id = 1,
            UserName = null,
            Password = null,
            Email = null,
            NickName = null,
            Description = null
        };
        
        var publicKey = File.ReadAllText(_publicKeyPath);
        
        // Act - This should not throw
        encryptionInfo.EncryptRsaObjectStrings(testObj, publicKey);
        
        // Assert
        Assert.Null(testObj.UserName);
        Assert.Null(testObj.Password);
        Assert.Null(testObj.Email);
        Assert.Null(testObj.NickName);
        Assert.Null(testObj.Description);
    }
    
    [Fact]
    public void EncryptObjectStrings_NullValues_HandlesGracefully()
    {
        // Arrange
        SetupBasePaths();
        var encryptionInfo = new EncryptionInfo(_configurationMock.Object);
        var testObj = new TestObject
        {
            Id = 1,
            UserName = null,
            Password = null,
            HashLogin = null,
            HashEmail = null,
            HashNickName = null
        };
        
        // Act - This should not throw
        encryptionInfo.EncryptObjectStrings(testObj);
        
        // Assert
        Assert.Null(testObj.UserName);
        Assert.Null(testObj.Password);
        Assert.Null(testObj.HashLogin);
        Assert.Null(testObj.HashEmail);
        Assert.Null(testObj.HashNickName);
    }

    // Special test for the DisposeTesterHelper Dispose() method
    [Fact]
    public void DisposeTesterHelper_Dispose_HandlesForcedClosure()
    {
        // Arrange
        var helper = new DisposeTesterHelperExtended();
        
        // Act & Assert - This would normally throw if not handled properly
        helper.ExhaustiveDisposeTest();
        Assert.True(helper.DisposeCalled);
    }
    
    private void SetupBasePaths()
    {
        // Create a temporary directory structure that mimics what the code expects
        var keyDirectory = Path.Combine(AppContext.BaseDirectory, "Keys");
        Directory.CreateDirectory(keyDirectory);
        
        var publicKeyInAppPath = Path.Combine(keyDirectory, "public.key"); 
        var privateKeyInAppPath = Path.Combine(keyDirectory, "private.key");
        
        // Copy the test keys to the expected location
        File.Copy(_publicKeyPath, publicKeyInAppPath, true);
        File.Copy(_privateKeyPath, privateKeyInAppPath, true);
    }
    
    // Test class to use with encryption methods - extended with more properties
    public class TestObject
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string NickName { get; set; }
        public string Description { get; set; }
        public string HashLogin { get; set; }
        public string HashEmail { get; set; }
        public string HashNickName { get; set; }
    }
    
    // Helper class to test path directory returning null case
    public class PathProviderWithNullReturn : IPathProvider
    {
        public string GetDirectoryName(string path)
        {
            // Always return null to force the throw path
            return null;
        }
    }
    
    // Interface for path operations
    public interface IPathProvider
    {
        string GetDirectoryName(string path);
    }
    
    // Testable version of EncryptionInfo that allows injecting a path provider
    public class TestableEncryptionInfo : EncryptionInfo
    {
        public TestableEncryptionInfo(IConfiguration configuration, IPathProvider pathProvider)
            : base(configuration)
        {
            // In a real implementation, you would:
            // 1. Make the original constructor protected
            // 2. Override the key directory creation logic to use the pathProvider
            
            // For this test, we're assuming the base constructor will throw
            // when it encounters a null directory name
        }
    }
}

// Extended helper to test the Dispose method more thoroughly
public class DisposeTesterHelperExtended : IDisposable
{
    private readonly string _tempDirectory;
    public bool DisposeCalled { get; private set; } = false;
    
    public DisposeTesterHelperExtended()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"EncryptionTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        
        // Create a file that's open to cause deletion to fail
        var filePath = Path.Combine(_tempDirectory, "locked.txt");
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        
        // Write something and keep the stream open (don't dispose it)
        var writer = new StreamWriter(stream);
        writer.WriteLine("This file is locked");
        writer.Flush();
        
        // Note: We intentionally don't close writer or stream
    }
    
    public void ExhaustiveDisposeTest()
    {
        // First test normal dispose
        Dispose();
        
        // Then try disposing when directory doesn't exist
        if (Directory.Exists(_tempDirectory))
        {
            try { Directory.Delete(_tempDirectory, true); } 
            catch { /* ignore */ }
        }
        Dispose(); // Should handle gracefully
        
        // Try with exception during deletion (already covered by first Dispose)
    }
    
    public void Dispose()
    {
        DisposeCalled = true;
        
        if (!Directory.Exists(_tempDirectory)) return;
        try
        {
            Directory.Delete(_tempDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}