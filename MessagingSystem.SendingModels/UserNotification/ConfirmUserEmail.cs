namespace MessagingSystem.SendingModels.UserNotification;

public class ConfirmUserEmail(string userName, string email, string nickName)
{
    public string UserName { get; set; } = userName;
    public string NickName { get; set; } = nickName;
    public string Email { get; set; } = email;
}