namespace MessagingSystem.Services.Notification.Core.User;

public class UserDto(string userName, string email, string nickName)
{
    public string Email { get; set; } = email;
    public string UserName { get; set; } = userName;
    public string NickName { get; set; } = nickName;
}