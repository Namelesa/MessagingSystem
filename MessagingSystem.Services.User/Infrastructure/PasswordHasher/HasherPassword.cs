using Microsoft.AspNetCore.Identity;

namespace MessagingSystem.Services.User.Infrastructure.PasswordHasher;

public class HasherPassword : IHasherPassword
{
    private readonly PasswordHasher<object> _passwordHasher = new();
    
    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(new object(), password);
    }
    
    public bool Verify(string hash, string password)
    {
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(new object(), hash, password);
            return result == PasswordVerificationResult.Success;
        }
        catch (FormatException)
        {
            Console.WriteLine("Format exception occurred while verifying the password hash.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return false;
        }
    }
}