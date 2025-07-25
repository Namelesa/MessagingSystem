using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.User;

public class UserOrchestrator(
    IUserImageRepository userImageRepository,
    IHasher hasher,
    IDecryptionInfo decryptionInfo
    ) : IUserOrchestrator
{
    public async Task<OperationResult<FoundedUser>> CheckUserAsync(string nickName)
    {
        var hashedNickName = hasher.Hash(nickName);
        var user = await userImageRepository.FindUserImageByHashAsync(hashedNickName);
        if (user == null)
            return OperationResult<FoundedUser>.Fail("User not found");

        var userImage = new FoundedUser(nickName, decryptionInfo.Decrypt(user.Image));
        
        return OperationResult<FoundedUser>.Ok(userImage);
    }
    public async Task<OperationResult<List<FoundedUser>>> CheckUsersAsync(List<string> nickNames)
    {
        if (nickNames.Count == 0)
            return OperationResult<List<FoundedUser>>.Fail("No nicknames provided");

        var tasks = nickNames.Select(async nickName =>
        {
            var hashedNickName = hasher.Hash(nickName);
            var user = await userImageRepository.FindUserImageByHashAsync(hashedNickName);
            return user != null 
                ? new FoundedUser(nickName, decryptionInfo.Decrypt(user.Image)) 
                : null;
        });

        var results = await Task.WhenAll(tasks);
        var foundUsers = results.Where(u => u != null).ToList();

        return foundUsers.Count == 0
            ? OperationResult<List<FoundedUser>>.Fail("No users found")
            : OperationResult<List<FoundedUser>>.Ok(foundUsers);
    }
    public async Task<OperationResult<string>> DeleteUserAsync(string nickNameHash)
    {
        var user = await FindUserAsync(nickNameHash); 
        await userImageRepository.DeleteUserImageAsync(user);
        return OperationResult<string>.Ok("User deleted successfully");
    }
    public async Task<OperationResult<string>> UpdateUserAsync(string nickName, string oldNickNameHash, string image)
    {
        var user = await FindUserAsync(oldNickNameHash);
        nickName = hasher.Hash(nickName);
        user.EditInfo(nickName, image);
        await userImageRepository.EditUserImageAsync(user);
        return OperationResult<string>.Ok("User updated successfully");
    }
    private async Task<UserImage> FindUserAsync(string nickNameHash)
    {
        var user = await userImageRepository.FindUserImageByHashAsync(nickNameHash);
        return user ?? new UserImage(hasher.Hash(nickNameHash), "");
    }
}