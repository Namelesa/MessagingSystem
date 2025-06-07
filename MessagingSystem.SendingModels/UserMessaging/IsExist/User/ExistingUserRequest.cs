namespace MessagingSystem.SendingModels.UserMessaging.IsExist.User;

public class ExistingUserRequest(string nickName)
{
    public string NickName { get; init; } = nickName;
}