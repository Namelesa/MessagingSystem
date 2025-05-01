using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application.Notification;

public interface INotificationOrchestrator
{
    Task<OperationResult<string>> SendConfirmEmailAsync(UserDto userDto, string nickName);
    Task<OperationResult<string>> SendEditUserInfoEmailAsync(UserDto userDto);
    Task<OperationResult<string>> SendDeleteUserInfoEmailAsync(UserDto userDto);

}