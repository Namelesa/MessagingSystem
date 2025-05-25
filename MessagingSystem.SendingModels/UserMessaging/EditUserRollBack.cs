namespace MessagingSystem.SendingModels.UserMessaging;

public class EditUserRollBack(bool isSuccess)
{
    public bool IsSuccess { get; set; } = isSuccess;
}