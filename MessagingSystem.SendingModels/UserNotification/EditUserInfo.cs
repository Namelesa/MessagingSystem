namespace MessagingSystem.SendingModels.UserNotification;

public class EditUserInfo(string email, string userName) : ConfirmUserEmail(userName, email)
{
    public DateTime DateOfChanges { get; set; } = DateTime.UtcNow;
}