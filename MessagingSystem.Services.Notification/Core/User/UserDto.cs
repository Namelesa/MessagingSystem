namespace MessagingSystem.Services.Notification.Core.User;

public class UserDto(string userName, string email)
{
    public string Email { get; set; } = email;
    public string UserName { get; set; } = userName;
}