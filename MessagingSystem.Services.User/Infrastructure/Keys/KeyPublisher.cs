using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;

namespace MessagingSystem.Services.User.Infrastructure.Keys;

public class KeyPublisher(IBus bus, IEncryptionInfo encryptInfo)
{
    public async Task PublishAsync()
    {
        var publicKey = encryptInfo.GetPublicKey();
        
        var publicKeyMessage = new PublicKeyMessage
        {
            PublicKey = encryptInfo.Encrypt(publicKey),
            ServiceName = encryptInfo.Encrypt("User")
        };
        await bus.Publish(publicKeyMessage);
    }
}