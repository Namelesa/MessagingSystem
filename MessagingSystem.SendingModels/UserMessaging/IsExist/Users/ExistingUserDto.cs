namespace MessagingSystem.SendingModels.UserMessaging.IsExist.Users;

public class ExistingUserDto(string nickName, bool isExist, string image)
{
    public string NickName { get; set; } = nickName;
    public string Image { get; set; } = image;
    public bool IsExist { get; set; } = isExist;
}