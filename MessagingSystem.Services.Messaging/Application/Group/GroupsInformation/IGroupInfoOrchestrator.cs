using MessagingSystem.Services.Messaging.Core.Groups.Group;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;

public interface IGroupInfoOrchestrator
{
    Task<OperationResult<GroupInfo>> CreateGroupAsync(GroupInfo groupInfo);
}