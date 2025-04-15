namespace MessagingSystem.Services.Notification.Core.User;

public interface INotification
{
    Task<bool> SendConfirmEmailAsync(UserDto userDto, string link);
    Task<bool> SendEditUserInfoEmailAsync(UserDto userDto);
    Task<bool> SendDeleteUserEmailAsync(UserDto userDto);
}