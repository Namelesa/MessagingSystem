using Microsoft.AspNetCore.Identity;

namespace MessagingSystem.Services.User.Infrastructure.PasswordHasher;

public class HasherPassword : IHasherPassword
{
    private readonly PasswordHasher<object> _passwordHasher = new();
    
    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(null, password);
    }
    
    public bool Verify(string hash, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(null, hash, password);
        return result == PasswordVerificationResult.Success;
    }
}