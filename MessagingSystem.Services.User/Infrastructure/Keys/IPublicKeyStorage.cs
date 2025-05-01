namespace MessagingSystem.Services.User.Infrastructure.Keys;

public interface IPublicKeyStorage
{
    void Save(string serviceName, string publicKey);
    string? Get(string serviceName);
}