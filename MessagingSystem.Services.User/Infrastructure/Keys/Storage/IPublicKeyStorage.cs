namespace MessagingSystem.Services.User.Infrastructure.Keys.Storage;

public interface IPublicKeyStorage
{
    void Save(string serviceName, string publicKey);
    string? Get(string serviceName);
}