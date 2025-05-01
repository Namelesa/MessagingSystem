using System.Security.Cryptography;
using System.Text;

namespace MessagingSystem.Services.User.Infrastructure.HasherInfo;

public class Hasher : IHasher
{
    public string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}