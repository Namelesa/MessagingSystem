using MessagingSystem.Services.Messaging.Application.User.Dto;

namespace MessagingSystem.Services.Messaging.Application.User;

public interface IUserOrchestrator
{
    Task<OperationResult<FoundedUser>> CheckUserAsync(string nickName);
    Task<OperationResult<List<FoundedUser>>> CheckUsersAsync(List<string> nickNames);
    Task<OperationResult<string>> DeleteUserAsync(string nickName);
    Task<OperationResult<string>> UpdateUserAsync(string nickName, string oldNickHash, string image);
}