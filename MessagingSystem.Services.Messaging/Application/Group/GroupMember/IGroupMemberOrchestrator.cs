namespace MessagingSystem.Services.Messaging.Application.Group.GroupMember;

public interface IGroupMemberOrchestrator
{
    Task<OperationResult<string>> UpdateMemberInfoAsync(string userHash, string newNickName, string image);
    Task<OperationResult<string>> DeleteMemberInfoAsync(string hashNickName);
}