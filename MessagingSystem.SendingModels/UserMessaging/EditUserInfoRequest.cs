namespace MessagingSystem.SendingModels.UserMessaging;

public class EditUserInfoRequest(string userHash, string userNickName)
{
    public string UserHash { get; set; } = userHash;
    public string UserNickName { get; set; } = userNickName;
}