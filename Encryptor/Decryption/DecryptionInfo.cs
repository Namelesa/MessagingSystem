using System.Security.Cryptography;
using System.Text;
using Encryptor.Publisher;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto.Parameters;
using ChaCha20Poly1305 = Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305;

namespace Encryptor.Decryption;

public class DecryptionInfo : IDecryptionInfo
{
    private readonly byte[] _key;
    private readonly RSA _rsa;
    
    public DecryptionInfo(IConfiguration configuration)
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
    
    public void DecryptObjectStrings<T>(T obj)
    {
        var excludedProps = new[] { "SenderHash", "RecipientHash" };

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
}