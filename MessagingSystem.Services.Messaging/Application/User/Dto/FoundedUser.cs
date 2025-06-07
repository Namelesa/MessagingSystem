namespace MessagingSystem.Services.Messaging.Application.User.Dto;

public class FoundedUser(string nickName, string? image)
{
    public string NickName { get; set; } = nickName;
    public string? Image { get; set; } = image;
}