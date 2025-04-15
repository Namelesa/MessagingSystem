namespace MessagingSystem.SendingModels.UserNotification;

public class DeleteUserEmail(string email, string userName)
{
    public string Email { get; set; } = email;
    public string UserName { get; set; } = userName;
}