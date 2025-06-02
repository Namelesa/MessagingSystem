namespace MessagingSystem.SendingModels.UserMessaging;

public class DeleteUserInfoRequest(string userNickNameHash)
{
    public string UserNickNameHash { get; init; } = userNickNameHash;
}