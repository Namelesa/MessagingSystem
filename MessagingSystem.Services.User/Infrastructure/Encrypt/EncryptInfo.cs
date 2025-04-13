using System.Security.Cryptography;
using System.Text;

namespace MessagingSystem.Services.User.Infrastructure.Encrypt;

public class EncryptInfo : IEncryptInfo
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptInfo(IConfiguration configuration)
    {
        var key = configuration["Encryption:AesKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Encryption key not configured");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        _iv = _key.Take(16).ToArray();
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    public void EncryptObjectStrings<T>(T obj)
    {
        var excludedProps = new[] { "Id" };

        var props = typeof(T).GetProperties()
            .Where(p => 
                p is { CanRead: true, CanWrite: true } &&
                p.PropertyType == typeof(string) &&
                !excludedProps.Contains(p.Name));

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
        var excludedProps = new[] { "Id" };

        var props = typeof(T).GetProperties()
            .Where(p => 
                p is { CanRead: true, CanWrite: true } &&
                p.PropertyType == typeof(string) &&
                !excludedProps.Contains(p.Name));

        foreach (var prop in props)
        {
            var value = prop.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(value))
            {
                prop.SetValue(obj, Decrypt(value));
            }
        }
    }

}