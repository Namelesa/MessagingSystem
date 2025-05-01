using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;

namespace MessagingSystem.Services.Notification.Application.Messaging.Key
{
    public class PublicKeyConsumer(IDecryptionInfo decryptInfo) : IConsumer<PublicKeyMessage>
    {
        public async Task Consume(ConsumeContext<PublicKeyMessage> context)
        {
            var message = context.Message;
            
            var serviceName = decryptInfo.Decrypt(message.ServiceName);
            
            if (serviceName == "Notification") 
                return;
            
            var publicKey = decryptInfo.Decrypt(message.PublicKey);
            
            SavePublicKeyToServiceFolder(serviceName, publicKey);

            await Task.CompletedTask;
        }
        
        private void SavePublicKeyToServiceFolder(string serviceName, string publicKey)
        {
            var serviceKeyFolder = Path.Combine(AppContext.BaseDirectory, "Key", serviceName);
            
            if (!Directory.Exists(serviceKeyFolder))
            {
                Directory.CreateDirectory(serviceKeyFolder);
            }
            
            var path = Path.Combine(serviceKeyFolder, "public.key");

            File.WriteAllText(path, publicKey);
        }
    }
}
