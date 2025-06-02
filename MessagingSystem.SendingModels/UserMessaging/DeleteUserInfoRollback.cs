namespace MessagingSystem.SendingModels.UserMessaging;

public class DeleteUserInfoRollback(string userNickNameHash)
{
    public string UserNickNameHash { get; set; } = userNickNameHash;
    public bool IsSuccess { get; set; }
}