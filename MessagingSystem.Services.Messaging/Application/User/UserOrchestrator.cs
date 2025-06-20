using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.User;

public class UserOrchestrator(
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IRequestClient<ExistingUserRequest> client,
    IRequestClient<ExistingUsersRequest> clients,
    IPublicKeyStorage publicKeyStorage,
    IUserImageRepository userImageRepository,
    IHasher hasher,
    IMapper mapper
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
        
        if(!response.Message.IsExist)
            return OperationResult<FoundedUser>.Fail("User not found");
        
        var image = new FoundedUser(hasher.Hash(
                decryptionInfo.Decrypt(response.Message.NickName)),
                response.Message.Image);
        var message = mapper.Map<UserImage>(image);
        
        try
        {
            await userImageRepository.AddUserImageAsync(message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<FoundedUser>.Fail("Error saving user image");
        }

        return OperationResult<FoundedUser>.Ok(
            new FoundedUser(decryptionInfo.Decrypt(response.Message.NickName),
                decryptionInfo.Decrypt(response.Message.Image)));
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

        var rawUsers = response.Message.Users.ToList();
        
        var users = (from user in rawUsers 
            where user.IsExist 
            let decryptedNick = decryptionInfo.Decrypt(decryptionInfo.DecryptRsa(user.NickName)) 
            let decryptedImage = decryptionInfo.Decrypt(decryptionInfo.DecryptRsa(user.Image)) 
            select new FoundedUser(decryptedNick, decryptedImage)).ToList();

        return users.Count > 0
            ? OperationResult<List<FoundedUser>>.Ok(users)
            : OperationResult<List<FoundedUser>>.Fail("No users found");
    }
    public async Task<OperationResult<string>> DeleteUserAsync(string nickName)
    {
        var user = await FindUserAsync(nickName); 
        await userImageRepository.DeleteUserImageAsync(user);
        return OperationResult<string>.Ok("User deleted successfully");
    }
    public async Task<OperationResult<string>> UpdateUserAsync(string nickName, string image)
    {
        var user = await FindUserAsync(nickName);
        user.EditInfo(nickName, image);
        await userImageRepository.EditUserImageAsync(user);
        return OperationResult<string>.Ok("User updated successfully");
    }
    private async Task<UserImage> FindUserAsync(string nickName)
    {
        var user = await userImageRepository.FindUserImageByHashAsync(nickName);
        return user ?? new UserImage(hasher.Hash(nickName), "");
    }
}