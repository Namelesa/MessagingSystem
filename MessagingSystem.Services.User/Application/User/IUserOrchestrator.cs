using MessagingSystem.Services.User.Application.User.Dto;

namespace MessagingSystem.Services.User.Application.User;

public interface IUserOrchestrator
{
    Task<OperationResult<string>> EditUserInfoAsync(UserDto userDto, string userId);
    Task<OperationResult<string>> DeleteUserAsync(string userId);
    Task<string> FindUserByNickNameAsync(string nickName);
}