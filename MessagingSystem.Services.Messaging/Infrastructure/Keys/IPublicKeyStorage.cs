namespace MessagingSystem.Services.Messaging.Infrastructure.Keys;

public interface IPublicKeyStorage
{
    void Save(string serviceName, string publicKey);
    string? Get(string serviceName);
}