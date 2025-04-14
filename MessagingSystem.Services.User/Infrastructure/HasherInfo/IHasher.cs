namespace MessagingSystem.Services.User.Infrastructure.HasherInfo;

public interface IHasher
{
    string Hash(string inputText);
}