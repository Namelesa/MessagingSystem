using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.PublicKey;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.Key
{
    public class PublicKeyConsumer(IDecryptionInfo encryptInfo, IPublicKeyStorage storage) : IConsumer<PublicKeyMessage>
    {
        public async Task Consume(ConsumeContext<PublicKeyMessage> context)
        {
            var message = context.Message;
            
            var serviceName = encryptInfo.Decrypt(message.ServiceName);
            
            if (serviceName == "Messaging") 
                return;

            Console.WriteLine($"[✓]:{serviceName}");
            
            var publicKey = encryptInfo.Decrypt(message.PublicKey);
            
            storage.Save(serviceName, publicKey);
            SavePublicKeyToServiceFolder(serviceName, publicKey);

            await Task.CompletedTask;
        }
        
        private static void SavePublicKeyToServiceFolder(string serviceName, string publicKey)
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