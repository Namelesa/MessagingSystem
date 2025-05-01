namespace MessagingSystem.Services.User.Infrastructure.PasswordHasher;

public interface IHasherPassword
{
    string Hash(string password);
    bool Verify(string hash, string password);
}