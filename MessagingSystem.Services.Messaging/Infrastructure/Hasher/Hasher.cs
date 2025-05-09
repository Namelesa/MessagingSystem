using System.Security.Cryptography;
using System.Text;

namespace MessagingSystem.Services.Messaging.Infrastructure.Hasher;

public class Hasher : IHasher
{
    public string Hash(string inputText)
    {
        var bytes = Encoding.UTF8.GetBytes(inputText.ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}