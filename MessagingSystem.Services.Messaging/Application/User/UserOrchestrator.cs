using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.User;

public class UserOrchestrator(
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IRequestClient<ExistingUserRequest> client,
    IRequestClient<ExistingUsersRequest> clients,
    IPublicKeyStorage publicKeyStorage
    ) : IUserOrchestrator
{
    public async Task<OperationResult<FoundedUser>> CheckUserAsync(string nickName)
    {
        var publicKey = publicKeyStorage.Get("User");

        if (publicKey == null)
            return OperationResult<FoundedUser>.Fail("Public key for User service not found");
        
        var encryptedNickName = encryptionInfo.Encrypt(nickName);
        encryptedNickName = encryptionInfo.EncryptRsa(encryptedNickName, publicKey);
        
        var response = await client.GetResponse<ExistingUserResponse>(
            new ExistingUserRequest(encryptedNickName));
        
        response.Message.NickName = decryptionInfo.DecryptRsa(response.Message.NickName);
        response.Message.Image = decryptionInfo.DecryptRsa(response.Message.Image);
        
        return response.Message.IsExist
            ? OperationResult<FoundedUser>.Ok(
                new FoundedUser(decryptionInfo.Decrypt(response.Message.NickName), 
                    decryptionInfo.Decrypt(response.Message.Image))) 
            : OperationResult<FoundedUser>.Fail("User not found");
    }
    public async Task<OperationResult<List<FoundedUser>>> CheckUsersAsync(List<string> nickNames)
    {
        var publicKey = publicKeyStorage.Get("User");

        if (publicKey == null)
            return OperationResult<List<FoundedUser>>.Fail("Public key for User service not found");
        
        var encryptedNickNames = nickNames
            .Select(nick =>
            {
                var encrypted = encryptionInfo.Encrypt(nick);
                return encryptionInfo.EncryptRsa(encrypted, publicKey);
            })
            .ToList();
        
        var request = new ExistingUsersRequest(encryptedNickNames);
        var response = await clients.GetResponse<ExistingUsersResponse>(request);

        var users = new List<FoundedUser>();

        foreach (var user in response.Message.Users)
        {
            if (!user.IsExist)
                continue;

            var decryptedNick = decryptionInfo.Decrypt(decryptionInfo.DecryptRsa(user.NickName));
            var decryptedImage = decryptionInfo.Decrypt(decryptionInfo.DecryptRsa(user.Image));

            users.Add(new FoundedUser(decryptedNick, decryptedImage));
        }

        return users.Count > 0
            ? OperationResult<List<FoundedUser>>.Ok(users)
            : OperationResult<List<FoundedUser>>.Fail("No users found");
    }
}