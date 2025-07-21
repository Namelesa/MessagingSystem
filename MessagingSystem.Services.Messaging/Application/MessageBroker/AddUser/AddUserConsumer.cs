using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Add;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.AddUser;

public class AddUserConsumer(
    IDecryptionInfo decryptionInfo,
    IUserImageRepository userImageRepository,
    IPublicKeyStorage publicKeyStorage,
    IEncryptionInfo encryptionInfo
    )
    : IConsumer<AddUserRequest>
{
    public async Task Consume(ConsumeContext<AddUserRequest> context)
    {
        var publicKey = publicKeyStorage.Get("User");

        if (publicKey == null)
        {
            Console.WriteLine("Key not founded");
            return;
        }
        
        var encryptNick = decryptionInfo.DecryptRsa(context.Message.NickNameHash);
        var encryptImage = decryptionInfo.DecryptRsa(context.Message.Image);

        var userImage = new UserImage(encryptNick, encryptImage);
        
        try
        {
            await userImageRepository.AddUserImageAsync(userImage);

            var response = new AddUserResponse(true);
        
            encryptionInfo.EncryptObjectStrings(response);
            encryptionInfo.EncryptRsaObjectStrings(response, publicKey);
        
            await context.RespondAsync(response);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}