namespace MessagingSystem.Services.Messaging.Infrastructure.Hasher;

public interface IHasher
{
    string Hash(string inputText);
}