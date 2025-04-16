using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.Notification.Infrastructure.Encrypt;

namespace MessagingSystem.Services.Notification.Infrastructure.KeyPublisher;

public class KeyPublisher(IBus bus, IEncryptInfo encryptInfo)
{
    public async Task PublishAsync()
    {
        var publicKey = encryptInfo.GetPublicKey();
        
        var publicKeyMessage = new PublicKeyMessage
        {
            PublicKey = encryptInfo.Encrypt(publicKey),
            ServiceName = encryptInfo.Encrypt("Notification")
        };
        await bus.Publish(publicKeyMessage);
    }
}