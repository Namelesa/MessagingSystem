using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.User;

public class UserOrchestrator(
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IRequestClient<ExistingUserRequest> client,
    IHasher hasher
    ) : IUserOrchestrator
{
    public async Task<string> CheckUserAsync(string nickName)
    {
        nickName = hasher.Hash(nickName);
        var encryptNick = encryptionInfo.Encrypt(nickName);
        
        var response = await client.GetResponse<ExistingUserResponse>(
            new ExistingUserRequest(encryptNick));
        return response.Message.IsExist == false 
            ? response.Message.NickName 
            : decryptionInfo.Decrypt(response.Message.NickName);
    }
}