namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

public class EditGroupDto(string groupName, string? image, string description)
{
    public string GroupName { get; init; } = groupName;
    public string? Image { get; init; } = image;
    public IFormFile? ImageFile { get; set; }
    public string Description { get; init; } = description;
}