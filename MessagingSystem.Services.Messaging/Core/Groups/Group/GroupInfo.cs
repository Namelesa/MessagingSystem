using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.Messaging.Core.Groups.Group;

public sealed class GroupInfo(string groupName, string? image, string description, string admin)
{
    public Guid Id { get; init; }
    public string GroupName { get; private set; } = groupName;
    public string? GroupNameHash { get; private set; }
    public string? Image { get; private set; } = image;
    public string Description { get; private set; } = description;
    public string Admin { get; private set; } = admin;
    public string? AdminHash { get; private set; }
    public List<GroupMembers> Members { get; private set; } = [];
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    private const int MaxMembersCount = 40;

    public void EditInfo(string groupName, string image, string description, string groupNameHash)
    {
        GroupName = groupName;
        Image = image;
        Description = description;
        GroupNameHash = groupNameHash;
    }
    
    public void AddUser(string userNick)
    {
        if (Members.Count >= MaxMembersCount)
            throw new InvalidOperationException("Maximum number of group members exceeded");

        if (Members.All(m => m.UserNickName != userNick))
            Members.Add(new GroupMembers(userNick) { GroupId = Id });
    }

    public void AddUsers(IEnumerable<string> userNicks)
    {
        var existing = new HashSet<string>(Members.Count, StringComparer.Ordinal);
        foreach (var m in Members)
        {
            if (!string.IsNullOrWhiteSpace(m.UserNickName))
                existing.Add(m.UserNickName);
        }

        foreach (var nick in userNicks)
        {
            if (Members.Count >= MaxMembersCount)
                throw new InvalidOperationException("Maximum number of group members exceeded");

            if (string.IsNullOrWhiteSpace(nick))
                continue;

            var cleanNick = nick.Trim();

            if (existing.Add(cleanNick))
            {
                Members.Add(new GroupMembers(cleanNick) { GroupId = Id });
            }
        }
    }
    
    public void ApplyHashToMembers(Func<string, string> hashFunc)
    {
        foreach (var member in Members.Where(m =>
                     !string.IsNullOrWhiteSpace(m.UserNickName) &&
                     string.IsNullOrWhiteSpace(m.UserNickNameHash)))
        {
            member.SetHash(hashFunc(member.UserNickName));
        }
    }
    
    public void EncryptMembers(Func<string, string> encryptFunc)
    {
        foreach (var member in Members.Where(member
                     => !string.IsNullOrWhiteSpace(member.UserNickName)))
            member.UserNickName = encryptFunc(member.UserNickName);
    }
    
    public void DeleteUsers(IEnumerable<string> userNicks)
    {
        var toRemove = new HashSet<string>(
            userNicks.Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim()),
            StringComparer.Ordinal);

        Members.RemoveAll(m => toRemove.Contains(m.UserNickName));
    }
    
    public void SetHash(string adminHash, string groupNameHash)
    {
        AdminHash = adminHash;
        GroupNameHash = groupNameHash;
    }
    public void SetAdminHash(string adminHash)
    {
        AdminHash = adminHash;
    }
    public void EditAdminNick(string adminNick)
    {
        Admin = adminNick;
    }
}