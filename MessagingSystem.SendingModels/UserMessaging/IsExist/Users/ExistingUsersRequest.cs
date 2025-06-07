namespace MessagingSystem.SendingModels.UserMessaging.IsExist.Users;

public class ExistingUsersRequest(List<string> nickNames)
{
    public List<string> NickNames { get; init; } = nickNames;
}