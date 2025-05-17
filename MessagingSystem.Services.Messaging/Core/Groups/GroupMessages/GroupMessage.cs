using MessagingSystem.Services.Messaging.Core.Groups.Group;

namespace MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;

public class GroupMessage(string sender, string content)
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public Guid? ReplyFor { get; private set; }
    public string Sender { get; private set; } = sender;
    public string? SenderHash { get; private set; }
    public string Content { get; private set; } = content;
    public DateTime SendTime { get; init; } = DateTime.UtcNow;
    public bool IsDeleted { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTime? EditTime { get; set; }
    public GroupInfo? Group { get; init; }
    
    public void SoftDeleteInfo()
    {
        IsDeleted = true;
    }
    
    public void EditInfo(string content)
    {
        IsEdited = true;
        Content = content;
        EditTime = DateTime.UtcNow;
    }

    public void Reply(Guid replyId)
    {
        ReplyFor = replyId;
    }

    public void SetHashes(string senderHash)
    {
        SenderHash = senderHash;
    }
}