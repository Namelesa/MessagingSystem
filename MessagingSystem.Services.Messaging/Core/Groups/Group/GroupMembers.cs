using System.Text.Json.Serialization;

namespace MessagingSystem.Services.Messaging.Core.Groups.Group;

public class GroupMembers(string userNickName)
{
    public Guid Id { get; init; }
    public string UserNickName { get; init; } = userNickName;
    public DateTime JoinedTime { get; init; } = DateTime.UtcNow;
    
    public Guid GroupId { get; init; }
    [JsonIgnore]
    public GroupInfo? Group { get; init; }
}