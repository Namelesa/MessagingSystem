namespace MessagingSystem.SendingModels.UserMessaging.Edit;

public class EditUserInfoRequest(string userHash, string userNickName, string image, string oldNickName)
{
    public string UserHash { get; set; } = userHash;
    public string OldNickName { get; set; } = oldNickName;
    public string UserNickName { get; set; } = userNickName;
    public string Image { get; set; } = image;
}