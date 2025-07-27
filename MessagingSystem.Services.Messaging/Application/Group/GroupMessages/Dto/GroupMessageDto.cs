namespace MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;

public class GroupMessageDto(string sender, string content, Guid groupId)
{
    public string Sender { get; init; } = sender;
    public string Content { get; init; } = content;
    public Guid GroupId { get; init; } = groupId;
    public DateTime SendTime { get; init; } = DateTime.UtcNow;
}