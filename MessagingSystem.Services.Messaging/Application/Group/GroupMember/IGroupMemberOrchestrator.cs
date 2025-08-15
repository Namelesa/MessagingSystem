namespace MessagingSystem.Services.Messaging.Application.Group.GroupMember;

public interface IGroupMemberOrchestrator
{
    Task<OperationResult<List<Guid>>> UpdateMemberInfoAsync(string userHash, string newNickName, string image);
    Task<OperationResult<List<Guid>>> DeleteMemberInfoAsync(string hashNickName);
}