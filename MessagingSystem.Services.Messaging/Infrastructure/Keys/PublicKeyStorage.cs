namespace MessagingSystem.Services.Messaging.Infrastructure.Keys;

public class PublicKeyStorage : IPublicKeyStorage
{
    private readonly Dictionary<string, string> _keys = new();

    public void Save(string serviceName, string publicKey)
    {
        _keys[serviceName] = publicKey;
    }

    public string? Get(string serviceName)
    {
        return _keys.GetValueOrDefault(serviceName);
    }
}