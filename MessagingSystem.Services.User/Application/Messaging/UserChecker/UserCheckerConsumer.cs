using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
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
        var publicKey = publicKeyStorage.Get("Messaging");

        if (publicKey == null)
        {
            Console.WriteLine("Key not founded");
            return;
        }
        
        var encryptNick = decryptionInfo.DecryptRsa(context.Message.NickName);
        var decryptNick = decryptionInfo.Decrypt(encryptNick);
        
        var nickName = hasher.Hash(decryptNick);
        
        var userResult = await userOrchestrator.FindUserByNickNameAsync(nickName);
        
        if(userResult.Data == null)
            return;
        
        var response = userResult.Success
            ? new ExistingUserResponse(
                decryptionInfo.Decrypt(userResult.Data.UserNickName),
                isExist: true,
                decryptionInfo.Decrypt(userResult.Data.Image))
            : new ExistingUserResponse("", isExist: false,"");
        
        encryptionInfo.EncryptObjectStrings(response);
        response.NickName = encryptionInfo.EncryptRsa(response.NickName, publicKey);
        response.Image = encryptionInfo.EncryptRsa(response.Image, publicKey);
        
        await context.RespondAsync(response);
    }
}