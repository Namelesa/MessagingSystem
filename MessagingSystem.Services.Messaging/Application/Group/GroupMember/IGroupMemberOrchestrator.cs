namespace MessagingSystem.Services.Messaging.Application.Group.GroupMember;

public interface IGroupMemberOrchestrator
{
    Task<OperationResult<string>> UpdateMemberInfoAsync(string userHash, string newNickName);
}