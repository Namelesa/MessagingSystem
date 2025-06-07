using System.Text.Json.Serialization;
using MessagingSystem.Services.Messaging.Core.Groups.Group;

namespace MessagingSystem.Services.Messaging.Core.Groups.GroupMember;

public class GroupMembers(string userNickName)
{
    public Guid Id { get; init; }
    public string UserNickName { get; set; } = userNickName;
    public string? UserNickNameHash { get; private set; }
    public DateTime JoinedTime { get; init; } = DateTime.UtcNow;

    public string? Image { get; private set; }
    public Guid GroupId { get; init; }
    [JsonIgnore]
    public GroupInfo? Group { get; init; }

    public void SetHash(string userNickNameHash)
    {
        UserNickNameHash = userNickNameHash;
    }
    public void SetImage(string image)
    {
        Image = image;
    }
}