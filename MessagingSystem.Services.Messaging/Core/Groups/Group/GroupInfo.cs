namespace MessagingSystem.Services.Messaging.Core.Groups.Group;

public class GroupInfo(string groupName, string image, string description, string admin)
{
    public Guid Id { get; init; }
    public string GroupName { get; private set; } = groupName;
    public string Image { get; private set; } = image;
    public string? Description { get; private set; } = description;
    public string Admin { get; init; } = admin;
    public string? AdminHash { get; private set; }
    public List<GroupMembers> Members { get; private set; } = [];

    public void EditInfo(string groupName, string image, string description)
    {
        GroupName = groupName;
        Image = image;
        Description = description;
    }
    
    public void AddUser(string userNick)
    {
        if (Members.All(m => m.UserNickName != userNick))
            Members.Add(new GroupMembers(userNick) { GroupId = Id });
    }

    public void DeleteUser(string userNick)
    {
        var member = Members.FirstOrDefault(m => m.UserNickName == userNick);
        if (member != null)
            Members.Remove(member);
    }
    
    public void SetHash(string adminHash)
    {
        AdminHash = adminHash;
    }
}