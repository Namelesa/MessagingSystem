using Encryptor.Encryption;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupMember;

public class GroupMemberOrchestrator(
    IGroupMembersRepository groupMembersRepository,
    IHasher hasher,
    IEncryptionInfo encryptionInfo
    ) : IGroupMemberOrchestrator
{
    public async Task<OperationResult<List<Guid>>> UpdateMemberInfoAsync(string userHash, string newNickName, string image)
    {
        var members = await groupMembersRepository.FindUserByHashAsync(userHash);
        var groupIds = members.Select(m => m.GroupId).Distinct().ToList();
        var newHash = hasher.Hash(newNickName);

        foreach (var member in members)
        {
            member.SetHash(newHash);
            member.UserNickName = encryptionInfo.Encrypt(newNickName);
            member.SetImage(encryptionInfo.Encrypt(image));
            await groupMembersRepository.EditUserInfoAsync(member);
        }

        return OperationResult<List<Guid>>.Ok(groupIds);
    }
    public async Task<OperationResult<List<Guid>>> DeleteMemberInfoAsync(string hashNickName)
    {
        try
        {
            var groupIds = await GetGroupIdsAsync(hashNickName);
            
            var result = await groupMembersRepository.DeleteUserInfoAsync(hashNickName);
            return result >= 0 
                ? OperationResult<List<Guid>>.Ok(groupIds) 
                : OperationResult<List<Guid>>.Fail("No rows were deleted. Possibly invalid user hash.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<List<Guid>>.Fail($"Exception occurred: {e.Message}");
        }
    }
    private async Task<List<Guid>> GetGroupIdsAsync(string userHash)
    {
        var members = await groupMembersRepository.FindUserByHashAsync(userHash);
        return members.Select(m => m.GroupId).Distinct().ToList();
    }
}