namespace MessagingSystem.Services.Messaging.Core.Groups.GroupMember;

public interface IGroupMembersRepository
{
    Task<List<GroupMembers>> FindUserByHashAsync(string userHash);
    Task<string> EditUserInfoAsync(GroupMembers groupMembers);
    Task<int> DeleteUserInfoAsync(string userHashName);
}