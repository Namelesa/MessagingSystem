using System.Security.Cryptography;
namespace MessagingSystem.Services.Notification.Infrastructure.KeyPublisher;

public class RsaKeyPair
{
    private string PublicKey { get; init; } = string.Empty;
    public string PrivateKey { get; private init; } = string.Empty;

    public static RsaKeyPair Generate()
    {
        using var rsa = RSA.Create(2048);
        return new RsaKeyPair
        {
            PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey()),
            PrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey())
        };
    }

    public static void SaveToFiles(RsaKeyPair pair, string publicKeyPath, string privateKeyPath)
    {
        File.WriteAllText(publicKeyPath, pair.PublicKey);
        File.WriteAllText(privateKeyPath, pair.PrivateKey);
    }

    public static RsaKeyPair LoadFromFiles(string publicKeyPath, string privateKeyPath)
    {
        if (!File.Exists(publicKeyPath) || !File.Exists(privateKeyPath))
            throw new FileNotFoundException("Rsa not founded");

        return new RsaKeyPair
        {
            PublicKey = File.ReadAllText(publicKeyPath),
            PrivateKey = File.ReadAllText(privateKeyPath)
        };
    }
}
