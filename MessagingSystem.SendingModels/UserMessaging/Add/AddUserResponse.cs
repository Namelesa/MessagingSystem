namespace MessagingSystem.SendingModels.UserMessaging.Add;

public class AddUserResponse(bool success)
{
    public bool Success { get; init; } = success;
}