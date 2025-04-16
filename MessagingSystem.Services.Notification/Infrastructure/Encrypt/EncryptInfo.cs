using System.Security.Cryptography;
using System.Text;
using MessagingSystem.Services.Notification.Infrastructure.KeyPublisher;
using Org.BouncyCastle.Crypto.Parameters;
using ChaCha20Poly1305 = Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305;

namespace MessagingSystem.Services.Notification.Infrastructure.Encrypt;

public class EncryptInfo : IEncryptInfo
{
    private readonly byte[] _key;
    private readonly RSA _rsa;

    public EncryptInfo(IConfiguration configuration)
    {
        var key = configuration["Encryption:ChaChaKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Encryption key not configured");
        
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        
        var publicPath = Path.Combine(AppContext.BaseDirectory, "Keys", "public.key");
        var privatePath = Path.Combine(AppContext.BaseDirectory, "Keys", "private.key");

        if (!File.Exists(publicPath) || !File.Exists(privatePath))
        {
            var generatedKeys = RsaKeyPair.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(publicPath) ?? throw new InvalidOperationException());
            RsaKeyPair.SaveToFiles(generatedKeys, publicPath, privatePath);
        }

        var rsaKeys = RsaKeyPair.LoadFromFiles(publicPath, privatePath);

        _rsa = RSA.Create();
        _rsa.ImportRSAPrivateKey(Convert.FromBase64String(rsaKeys.PrivateKey), out _);
    }

    public string Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plainText);

        var cipher = new ChaCha20Poly1305();
        var parameters = new AeadParameters(new KeyParameter(_key), 128, nonce, null);
        cipher.Init(true, parameters);

        var output = new byte[cipher.GetOutputSize(plaintextBytes.Length)];
        var len = cipher.ProcessBytes(plaintextBytes, 0, plaintextBytes.Length, output, 0);
        cipher.DoFinal(output, len);
        
        var result = new byte[nonce.Length + output.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(output, 0, result, nonce.Length, output.Length);

        return Convert.ToBase64String(result);
    }

    public string EncryptRsa(string plainText, string baseKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(baseKey), out _);
        var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string cipherText)
    {
        var input = Convert.FromBase64String(cipherText);
        var nonce = input[..12];
        var ciphertextBytes = input[12..];

        var cipher = new ChaCha20Poly1305();
        var parameters = new AeadParameters(new KeyParameter(_key), 128, nonce, null);
        cipher.Init(false, parameters);

        var output = new byte[cipher.GetOutputSize(ciphertextBytes.Length)];
        var len = cipher.ProcessBytes(ciphertextBytes, 0, ciphertextBytes.Length, output, 0);
        cipher.DoFinal(output, len);

        return Encoding.UTF8.GetString(output);
    }

    public string DecryptRsa(string baseKey)
    {
        var encryptedBytes = Convert.FromBase64String(baseKey);
        var decrypted = _rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(decrypted);
    }

    public void EncryptObjectStrings<T>(T obj)
    {
        var props = typeof(T).GetProperties()
            .Where(p => 
                p is { CanRead: true, CanWrite: true } &&
                p.PropertyType == typeof(string));

        foreach (var prop in props)
        {
            var value = prop.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(value))
            {
                prop.SetValue(obj, Encrypt(value));
            }
        }
    }

    public void DecryptObjectStrings<T>(T obj)
    {
        var props = typeof(T).GetProperties()
            .Where(p => 
                p is { CanRead: true, CanWrite: true } &&
                p.PropertyType == typeof(string));

        foreach (var prop in props)
        {
            var value = prop.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(value))
            {
                prop.SetValue(obj, Decrypt(value));
            }
        }
    }
    
    public void DecryptRsaObjectStrings<T>(T obj)
    {
        var props = typeof(T).GetProperties()
            .Where(p => 
                p is { CanRead: true, CanWrite: true } &&
                p.PropertyType == typeof(string));

        foreach (var prop in props)
        {
            var value = prop.GetValue(obj) as string;
            if (string.IsNullOrEmpty(value)) continue;
            try
            {
                var decrypted = DecryptRsa(value);
                prop.SetValue(obj, decrypted);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    
    public string GetPublicKey()
    {
        var publicKeyPath = Path.Combine(AppContext.BaseDirectory, "Keys", "public.key");

        if (!File.Exists(publicKeyPath))
        {
            throw new FileNotFoundException("Public key not found");
        }

        return File.ReadAllText(publicKeyPath);
    }
}