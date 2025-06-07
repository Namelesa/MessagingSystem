namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

public class GroupDto(
    string groupName, 
    string? image, 
    string description, 
    string admin, 
    List<string> users,
    byte[] rowVersion)
{
    public string GroupName { get; init; } = groupName;
    public string? Image { get; init; } = image;
    public string Description { get; init; } = description;
    public string Admin { get; private set; } = admin;
    public List<string> Users { get; private set; } = users;
    public List<UserInGroupDto>? Members { get; private set; }
    public string RowVersion => Convert.ToBase64String(rowVersion);

    public void AddAdminLikeUser(string admin)
    {
        Admin = admin;
        Users.Add(Admin);
    }
    
    public void SetMembers(List<UserInGroupDto> members)
    {
        Members = members;
    }
}