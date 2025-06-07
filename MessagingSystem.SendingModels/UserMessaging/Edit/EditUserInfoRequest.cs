namespace MessagingSystem.SendingModels.UserMessaging.Edit;

public class EditUserInfoRequest(string userHash, string userNickName, string image)
{
    public string UserHash { get; set; } = userHash;
    public string UserNickName { get; set; } = userNickName;
    public string Image { get; set; } = image;
}