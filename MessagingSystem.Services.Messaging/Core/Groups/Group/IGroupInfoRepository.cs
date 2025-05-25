namespace MessagingSystem.Services.Messaging.Core.Groups.Group;

public interface IGroupInfoRepository
{
    Task<GroupInfo?> FindGroupByIdAsync(Guid id);
    Task<GroupInfo?> FindGroupByNameHashAsync(string groupName);
    Task<List<GroupInfo>?> FindGroupByAdminHashAsync(string adminHash);
    Task<GroupInfo> EditGroupInfoAsync(GroupInfo groupInfo);
    Task<GroupInfo> DeleteGroupAsync(GroupInfo groupInfo);
    Task<GroupInfo> CreateGroupAsync(GroupInfo groupInfo);
    Task<List<GroupInfo>> GetGroupsByUserAsync(string userHash);
}