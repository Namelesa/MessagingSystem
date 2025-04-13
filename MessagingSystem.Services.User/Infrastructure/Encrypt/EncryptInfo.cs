using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using ChaCha20Poly1305 = Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305;

namespace MessagingSystem.Services.User.Infrastructure.Encrypt;

public class EncryptInfo : IEncryptInfo
{
    private readonly byte[] _key;

    public EncryptInfo(IConfiguration configuration)
    {
        var key = configuration["Encryption:ChaChaKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Encryption key not configured");
        
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
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
