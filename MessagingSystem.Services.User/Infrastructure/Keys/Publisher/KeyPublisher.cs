using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.User.Infrastructure.Encrypt;

namespace MessagingSystem.Services.User.Infrastructure.Keys.Publisher;

public class KeyPublisher(IBus bus, IEncryptInfo encryptInfo)
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