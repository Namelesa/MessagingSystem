using MessagingSystem.Services.User.Application.User.Dto;

namespace MessagingSystem.Services.User.Application.User;

public interface IUserOrchestrator
{
    Task<OperationResult<string>> EditUserInfoAsync(UserDto userDto, string userId);
    Task<OperationResult<string>> DeleteUserAsync(string userId);
    Task<OperationResult<UserFoundDto>> FindUserByNickNameAsync(string nickName);
    Task<OperationResult<List<UserFoundDto>>> FindUsersByNickNamesAsync(List<string> nickNames);
}