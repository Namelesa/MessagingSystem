using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.Notification.Infrastructure.Encrypt;

namespace MessagingSystem.Services.Notification.Application.Messaging.Key
{
    public class PublicKeyConsumer(IEncryptInfo encryptInfo) : IConsumer<PublicKeyMessage>
    {
        public async Task Consume(ConsumeContext<PublicKeyMessage> context)
        {
            var message = context.Message;
            
            var serviceName = encryptInfo.Decrypt(message.ServiceName);
            
            if (serviceName == "Notification") 
                return;

            Console.WriteLine($"[✓]:{serviceName}");
            
            var publicKey = encryptInfo.Decrypt(message.PublicKey);
            
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
