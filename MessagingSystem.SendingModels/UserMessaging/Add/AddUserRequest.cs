namespace MessagingSystem.SendingModels.UserMessaging.Add;

public class AddUserRequest(string nickNameHash, string image)
{
    public string NickNameHash { get; init; } = nickNameHash;
    public string Image { get; init; } = image;
}