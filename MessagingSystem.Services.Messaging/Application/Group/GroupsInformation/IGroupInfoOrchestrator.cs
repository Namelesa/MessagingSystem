using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;

public interface IGroupInfoOrchestrator
{
    Task<OperationResult<GroupDto>> CreateGroupAsync(GroupDto groupInfo);
    Task<OperationResult<GroupDto>> FindGroupByNameAsync(string groupName);
    Task<OperationResult<GroupDto>> FindGroupByIdAsync(Guid id);
    Task<OperationResult<string>> EditGroupsAdminAsync(string adminHash, string newAdminNick);
    Task<OperationResult<GroupDto>> EditGroupInfoAsync(Guid id, EditGroupDto groupInfo);
    Task<OperationResult<string>> DeleteGroupInfoAsync(Guid id, string adminHash);
    Task<OperationResult<GroupDto>> AddMembersToGroupAsync(Guid id, GroupMembersDto groupMembersDto, string adminHash);
    Task<OperationResult<GroupDto>> DeleteMembersFromGroupAsync(Guid id, GroupMembersDto groupMembersDto, 
        string adminHash);
    Task<OperationResult<List<GroupDto>>> GetGroupsForUserAsync(string userNick);
}