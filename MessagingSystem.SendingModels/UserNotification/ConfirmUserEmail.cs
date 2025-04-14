namespace MessagingSystem.SendingModels.UserNotification;

public class ConfirmUserEmail(string userName, string email)
{
    public string UserName { get; set; } = userName;
    public string Email { get; set; } = email;
}