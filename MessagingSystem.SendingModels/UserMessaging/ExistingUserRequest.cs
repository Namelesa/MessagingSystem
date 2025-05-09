namespace MessagingSystem.SendingModels.UserMessaging;

public class ExistingUserRequest(string nickName)
{
    public string NickName { get; init; } = nickName;
}