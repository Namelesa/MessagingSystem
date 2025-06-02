using Encryptor.Encryption;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupMember;

public class GroupMemberOrchestrator(
    IGroupMembersRepository groupMembersRepository,
    IHasher hasher,
    IEncryptionInfo encryptionInfo
    ) : IGroupMemberOrchestrator
{
    public async Task<OperationResult<string>> UpdateMemberInfoAsync(string userHash, string newNickName)
    {
        var members = await groupMembersRepository.FindUserByHashAsync(userHash);
        
        if(members == null)
            return OperationResult<string>.Fail("User not found");
        
        var newHash = hasher.Hash(newNickName);

        foreach (var member in members)
        {
            member.SetHash(newHash);
            member.UserNickName = encryptionInfo.Encrypt(member.UserNickName);
            await groupMembersRepository.EditUserInfoAsync(member);
        }

        return OperationResult<string>.Ok("User info is updated");
    }
    public async Task<OperationResult<string>> DeleteMemberInfoAsync(string hashNickName)
    {
        try
        {
            var result = await groupMembersRepository.DeleteUserInfoAsync(hashNickName);
            return result > 0 
                ? OperationResult<string>.Ok($"Delete successful. Rows affected: {result}") 
                : OperationResult<string>.Fail("No rows were deleted. Possibly invalid user hash.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"Exception occurred: {e.Message}");
        }
    }
}