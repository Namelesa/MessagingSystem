namespace MessagingSystem.SendingModels.UserMessaging;

public class ExistingUserResponse(string nickName, bool isExist)
{
    public string NickName { get; set; } = nickName;
    public bool IsExist { get; set; } = isExist;
}