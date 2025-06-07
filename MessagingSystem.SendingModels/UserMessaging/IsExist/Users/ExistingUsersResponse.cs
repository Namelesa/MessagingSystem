namespace MessagingSystem.SendingModels.UserMessaging.IsExist.Users;

public class ExistingUsersResponse(List<ExistingUserDto> users)
{
    public List<ExistingUserDto> Users { get; set; } = users;
}