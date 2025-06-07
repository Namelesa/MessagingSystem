namespace MessagingSystem.Services.User.Application.User.Dto;

public class UserFoundDto(string nickName, string? image)
{
    public string UserNickName { get; set; } = nickName;
    public string? Image { get; set; } = image;
}