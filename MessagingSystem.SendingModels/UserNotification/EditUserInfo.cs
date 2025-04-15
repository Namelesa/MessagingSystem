namespace MessagingSystem.SendingModels.UserNotification;

public class EditUserInfo(string email, string userName, string nickName) : ConfirmUserEmail(userName, email, nickName)
{
    public DateTime DateOfChanges { get; set; } = DateTime.UtcNow;
}