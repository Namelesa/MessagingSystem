namespace MessagingSystem.SendingModels.UserMessaging.Edit;

public class EditUserRollBack(bool isSuccess)
{
    public bool IsSuccess { get; set; } = isSuccess;
}