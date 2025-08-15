namespace MessagingSystem.SendingModels.UserMessaging.Delete;

public class DeleteUserInfoRequest(string userNickNameHash, string userNickName)
{
    public string UserNickNameHash { get; init; } = userNickNameHash;
    public string UserNickName { get; init; } = userNickName;
}