using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;

public class GroupInfoOrchestrator(
    IGroupInfoRepository groupInfoRepository,
    IHasher hasher
    ) : IGroupInfoOrchestrator
{
    public async Task<OperationResult<GroupInfo>> CreateGroupAsync(GroupInfo groupInfo)
    {
        var adminHash = hasher.Hash(groupInfo.Admin);
        groupInfo.SetHash(adminHash);
        groupInfo.AddUser("test");
        var result = await groupInfoRepository.CreateGroupAsync(groupInfo);

        return result != null
            ? OperationResult<GroupInfo>.Ok(result)
            : OperationResult<GroupInfo>.Fail("Can not create group");
    }
}