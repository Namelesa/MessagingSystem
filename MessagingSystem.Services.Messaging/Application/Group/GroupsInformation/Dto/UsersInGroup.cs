namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

public class UserInGroupDto(string nickName, string? image = null)
{
    public string NickName { get; set; } = nickName;
    public string? Image { get; set; } = image;
}
