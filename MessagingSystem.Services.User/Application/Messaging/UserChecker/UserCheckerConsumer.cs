using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;

namespace MessagingSystem.Services.User.Application.Messaging.UserChecker;

public class UserCheckerConsumer(
    IDecryptionInfo decryptionInfo,
    IUserOrchestrator userOrchestrator,
    IPublicKeyStorage publicKeyStorage,
    IEncryptionInfo encryptionInfo,
    IHasher hasher
    ) : IConsumer<ExistingUserRequest>
{
    public async Task Consume(ConsumeContext<ExistingUserRequest> context)
    {
        var publicKey = publicKeyStorage.Get("MessageBroker");

        if (publicKey == null)
        {
            Console.WriteLine("Key not founded");
            return;
        }
        
        var encryptNick = decryptionInfo.DecryptRsa(context.Message.NickName); 
        var decryptNick = decryptionInfo.Decrypt(encryptNick);

        var nickName = hasher.Hash(decryptNick);
        
        var userNickName = await userOrchestrator.FindUserByNickNameAsync(nickName);
        
        var isExist = !userNickName.Contains("not Found");
        
        var response = new ExistingUserResponse(userNickName, isExist);
        encryptionInfo.EncryptObjectStrings(response);
        encryptionInfo.EncryptRsaObjectStrings(response, publicKey);
        
        await context.RespondAsync(response);
    }
}