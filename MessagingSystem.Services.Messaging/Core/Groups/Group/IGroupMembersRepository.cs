namespace MessagingSystem.Services.Messaging.Core.Groups.Group;

public interface IGroupMembersRepository
{
    Task<List<GroupMembers>?> FindUserByHashAsync(string userHash);
    Task<string> EditUserInfoAsync(GroupMembers groupMembers);
    Task<int> DeleteUserInfoAsync(string userHashName);
}