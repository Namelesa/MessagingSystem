namespace MessagingSystem.SendingModels.UserNotification;

public class EditUserEmail(string email, string userName)
{
    public string Email { get; set; } = email;
    public string UserName { get; set; } = userName;
}