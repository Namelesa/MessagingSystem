namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

public class GroupMembersDto()
{
    public List<string> Users { get; set; } = [];
    public string Admin { get; set; }
}