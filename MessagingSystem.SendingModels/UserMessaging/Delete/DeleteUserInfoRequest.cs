namespace MessagingSystem.SendingModels.UserMessaging.Delete;

public class DeleteUserInfoRequest(string userNickNameHash)
{
    public string UserNickNameHash { get; init; } = userNickNameHash;
}