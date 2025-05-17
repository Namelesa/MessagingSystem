using System.Text;
using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.User;

public class UserOrchestrator(
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IRequestClient<ExistingUserRequest> client,
    IPublicKeyStorage publicKeyStorage
    ) : IUserOrchestrator
{
    public async Task<OperationResult<string>> CheckUserAsync(string nickName)
    {
        var publicKey = publicKeyStorage.Get("User");

        if (publicKey == null)
            return OperationResult<string>.Fail("Public key for User service not found");
        
        var encryptedNickName = encryptionInfo.Encrypt(nickName);
        var bytes = Encoding.UTF8.GetBytes(encryptedNickName);
        Console.WriteLine($"[Length before RSA]: {bytes.Length}");
        encryptedNickName = encryptionInfo.EncryptRsa(encryptedNickName, publicKey);
        
        var response = await client.GetResponse<ExistingUserResponse>(
            new ExistingUserRequest(encryptedNickName));
        
        decryptionInfo.DecryptRsaObjectStrings(response);
        decryptionInfo.DecryptObjectStrings(response);

        return response.Message.IsExist
            ? OperationResult<string>.Ok(decryptionInfo.Decrypt(response.Message.NickName)) 
            : OperationResult<string>.Fail("User not found");
    }
}