using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Encryptor.Decryption;
using Microsoft.Extensions.Configuration;
using Moq;
using Org.BouncyCastle.Crypto.Parameters;
using ChaCha20Poly1305 = Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305;

namespace MessagingSystem.Tests.Encryptor.Decrypt;

public class DecryptionInfoFullCoverageTests
{
    private const string TestKey = "TestEncryptionKey123";
    private readonly Mock<IConfiguration> _mockConfig;

    public DecryptionInfoFullCoverageTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Encryption:ChaChaKey"]).Returns(TestKey);

        var keyDir = Path.Combine(AppContext.BaseDirectory, "Keys");
        if (Directory.Exists(keyDir))
            Directory.Delete(keyDir, true);
    }

    [Fact]
    public void Constructor_WithValidKey_CreatesKeysAndInitializesRsa()
    {
        var decryptionInfo = new DecryptionInfo(_mockConfig.Object);

        Assert.NotNull(decryptionInfo);
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Keys", "public.key")));
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Keys", "private.key")));
    }

    [Fact]
    public void Constructor_MissingKey_ThrowsException()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["Encryption:ChaChaKey"]).Returns((string)null);

        Assert.Throws<InvalidOperationException>(() => new DecryptionInfo(mock.Object));
    }

    [Fact]
    public void Constructor_EmptyKey_ThrowsException()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["Encryption:ChaChaKey"]).Returns("   ");

        Assert.Throws<InvalidOperationException>(() => new DecryptionInfo(mock.Object));
    }

    [Fact]
    public void Decrypt_ValidChaChaEncryptedText_ReturnsOriginal()
    {
        var decryptionInfo = new DecryptionInfo(_mockConfig.Object);
        var plainText = "secret message";
        var encrypted = EncryptChaCha(plainText, TestKey);

        var result = decryptionInfo.Decrypt(encrypted);

        Assert.Equal(plainText, result);
    }

    [Fact]
    public void DecryptRsa_ValidEncrypted_ReturnsOriginal()
    {
        var decryptionInfo = new DecryptionInfo(_mockConfig.Object);
        var rsaField = typeof(DecryptionInfo).GetField("_rsa", BindingFlags.NonPublic | BindingFlags.Instance);
        var rsa = (RSA)rsaField.GetValue(decryptionInfo);
        
        var plain = "rsa message";
        var encrypted = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(plain), RSAEncryptionPadding.OaepSHA256));

        var decrypted = decryptionInfo.DecryptRsa(encrypted);

        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void DecryptObjectStrings_DecryptsAllStrings()
    {
        var decryptionInfo = new DecryptionInfo(_mockConfig.Object);
        var obj = new TestObj
        {
            Name = EncryptChaCha("Alice", TestKey),
            Email = EncryptChaCha("alice@example.com", TestKey),
            Empty = "",
            NullValue = null
        };

        decryptionInfo.DecryptObjectStrings(obj);

        Assert.Equal("Alice", obj.Name);
        Assert.Equal("alice@example.com", obj.Email);
        Assert.Equal("", obj.Empty);
        Assert.Null(obj.NullValue);
    }

    [Fact]
    public void DecryptRsaObjectStrings_DecryptsValidIgnoresInvalid()
    {
        var decryptionInfo = new DecryptionInfo(_mockConfig.Object);
        var rsaField = typeof(DecryptionInfo).GetField("_rsa", BindingFlags.NonPublic | BindingFlags.Instance);
        var rsa = (RSA)rsaField.GetValue(decryptionInfo);

        var obj = new TestObj
        {
            Name = Convert.ToBase64String(rsa.Encrypt("Alice"u8.ToArray(), RSAEncryptionPadding.OaepSHA256)),
            Email = "not_base64",
            Empty = "",
            NullValue = null
        };

        var originalOut = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);

        decryptionInfo.DecryptRsaObjectStrings(obj);
        Console.SetOut(originalOut);

        Assert.Equal("Alice", obj.Name);
        Assert.Equal("not_base64", obj.Email);
        Assert.Contains("Exception", output.ToString());
    }

    private static string EncryptChaCha(string plainText, string key)
    {
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var messageBytes = Encoding.UTF8.GetBytes(plainText);
        var cipher = new ChaCha20Poly1305();
        var parameters = new AeadParameters(new KeyParameter(keyBytes), 128, nonce, null);

        cipher.Init(true, parameters);

        byte[] cipherBytes = new byte[cipher.GetOutputSize(messageBytes.Length)];
        int len = cipher.ProcessBytes(messageBytes, 0, messageBytes.Length, cipherBytes, 0);
        cipher.DoFinal(cipherBytes, len);

        byte[] full = new byte[nonce.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, full, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, full, nonce.Length, cipherBytes.Length);

        return Convert.ToBase64String(full);
    }

    private class TestObj
    {
        public string Name { get; init; }
        public string Email { get; init; }
        public string Empty { get; init; }
        public string NullValue { get; init; }
    }
}
